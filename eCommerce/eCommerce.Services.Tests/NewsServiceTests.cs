using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using eCommerce.Model.Exceptions;
using eCommerce.Model.Requests;
using eCommerce.Model.SearchObjects;
using eCommerce.Services.Database;
using eCommerce.Services.Images;
using eCommerce.Services.Validators;
using FluentValidation;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace eCommerce.Services.Tests
{
    public class NewsServiceTests : IDisposable
    {
        private readonly string _uploads = Path.Combine(Path.GetTempPath(), "wildnest-news-tests", Guid.NewGuid().ToString("N"));

        public NewsServiceTests()
        {
            Directory.CreateDirectory(_uploads);
        }

        public void Dispose()
        {
            if (Directory.Exists(_uploads))
            {
                Directory.Delete(_uploads, recursive: true);
            }
        }

        [Fact]
        public void SeedImages_ArePngFiles_TheMediaEndpointCanRead()
        {
            NewsSeedImages.EnsurePresent(_uploads);

            foreach (var name in new[]
            {
                NewsSeedImages.SafariFileName,
                NewsSeedImages.PandaFileName,
                NewsSeedImages.HabitatFileName,
                NewsSeedImages.DraftFileName
            })
            {
                var bytes = File.ReadAllBytes(Path.Combine(_uploads, name));
                Assert.Equal("image/png", ImageContent.Detect(bytes));
                var stored = new FileStoredImageService(new UploadSettings(_uploads)).Read(name);
                Assert.NotNull(stored);
                Assert.Equal("image/png", stored!.ContentType);
            }
        }

        [Fact]
        public void SolidPng_InflatesToTheRequestedColor()
        {
            var png = NewsSeedImages.SolidPng(10, 20, 30);
            var offset = 8;
            byte[]? idat = null;
            while (offset + 12 <= png.Length)
            {
                var length = BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(offset, 4));
                var type = Encoding.ASCII.GetString(png, offset + 4, 4);
                if (type == "IDAT")
                {
                    idat = png.AsSpan(offset + 8, length).ToArray();
                    break;
                }

                offset += 12 + length;
            }

            Assert.NotNull(idat);
            using var input = new MemoryStream(idat!);
            using var zlib = new ZLibStream(input, CompressionMode.Decompress);
            using var raw = new MemoryStream();
            zlib.CopyTo(raw);
            var pixels = raw.ToArray();
            Assert.Equal(new byte[] { 0, 10, 20, 30 }, pixels.Take(4));
        }

        [Fact]
        public async Task PublishedFeed_IsNewestFirst_AndHidesDrafts()
        {
            using var db = new SqliteDb();
            var service = Service(db.Context, userId: 1);
            await service.InsertAsync(new NewsInsertRequest
            {
                Title = "Latest bulletin",
                Text = "This item is newer than the seeded announcements.",
                ImageBase64 = Convert.ToBase64String(NewsSeedImages.SolidPng(9, 9, 9)),
                ImageContentType = "image/png",
                PublishedAt = new DateTime(2026, 10, 18, 12, 0, 0, DateTimeKind.Utc),
                IsPublished = true
            });

            var page = await service.GetPublishedAsync(new NewsSearchObject
            {
                Page = 1,
                PageSize = 10,
                IncludeTotalCount = true,
                IsPublished = false,
                SortBy = "Id asc"
            });

            Assert.Equal(4, page.TotalCount);
            Assert.Equal("Latest bulletin", page.Items[0].Title);
            Assert.Equal(new[] { 1, 2, 3 }, page.Items.Skip(1).Select(item => item.Id).ToArray());
            Assert.All(page.Items, item => Assert.DoesNotContain("draft", item.Title, StringComparison.OrdinalIgnoreCase));
            Assert.All(page.Items, item => Assert.False(string.IsNullOrWhiteSpace(item.ImageUrl)));
            Assert.Equal("Alice Admin", page.Items[0].AuthorName);
            Assert.DoesNotContain(page.Items, item => item.Excerpt.Length > 183);

            var hidden = await Assert.ThrowsAsync<NotFoundException>(() => service.GetPublishedByIdAsync(4));
            Assert.Equal("News item not found.", hidden.Message);

            var draft = await service.GetByIdAsync(4);
            Assert.False(draft.IsPublished);
            Assert.Equal("Alice Admin", draft.AuthorName);
        }

        [Fact]
        public async Task Insert_StoresImage_AndUsesTheSignedInAuthor()
        {
            using var db = new SqliteDb();
            var images = new FileStoredImageService(new UploadSettings(_uploads));
            var service = Service(db.Context, userId: 2, images: images);
            var when = new DateTime(2026, 10, 18, 15, 45, 0, DateTimeKind.Utc);

            var created = await service.InsertAsync(new NewsInsertRequest
            {
                Title = "  Keeper talk on Saturday  ",
                Text = "Meet the keepers by the wetland habitat at 11:00 AM.",
                ImageBase64 = Convert.ToBase64String(NewsSeedImages.SolidPng(10, 20, 30)),
                ImageContentType = "image/png",
                PublishedAt = when,
                IsPublished = true
            });

            Assert.Equal("Keeper talk on Saturday", created.Title);
            Assert.Equal("Bob Admin", created.AuthorName);
            Assert.StartsWith("/media/", created.ImageUrl);
            Assert.Equal(DateTimeKind.Utc, created.PublishedAt.Kind);
            Assert.Equal(when, created.PublishedAt);
            Assert.True(File.Exists(Path.Combine(_uploads, created.ImageUrl["/media/".Length..])));

            db.Context.ChangeTracker.Clear();
            var stored = await db.Context.NewsItems.AsNoTracking().SingleAsync(item => item.Id == created.Id);
            Assert.Equal(2, stored.AuthorUserId);
        }

        [Fact]
        public async Task Insert_WithoutImage_DoesNotWriteAFile()
        {
            using var db = new SqliteDb();
            var service = Service(db.Context, userId: 1);

            var error = await Assert.ThrowsAsync<ValidationException>(() => service.InsertAsync(new NewsInsertRequest
            {
                Title = "Missing picture",
                Text = "An image is part of every news item.",
                PublishedAt = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc)
            }));

            Assert.Contains(error.Errors, failure => failure.ErrorMessage == NewsService.ImageRequiredMessage);
            Assert.False(Directory.EnumerateFiles(_uploads).Any());
        }

        [Fact]
        public async Task Update_ReplacesTheImage_AndDeleteRemovesTheFile()
        {
            using var db = new SqliteDb();
            var images = new FileStoredImageService(new UploadSettings(_uploads));
            var service = Service(db.Context, userId: 1, images: images);
            var created = await service.InsertAsync(ValidInsert("Morning feed"));
            var firstFile = Path.Combine(_uploads, created.ImageUrl["/media/".Length..]);

            var updated = await service.UpdateAsync(created.Id, new NewsUpdateRequest
            {
                Title = "Morning feed",
                Text = "The morning feed is moved to 9:30 AM.",
                ImageBase64 = Convert.ToBase64String(NewsSeedImages.SolidPng(200, 10, 10)),
                ImageContentType = "image/png",
                PublishedAt = new DateTime(2026, 10, 2, 9, 30, 0, DateTimeKind.Utc),
                IsPublished = false
            });

            Assert.NotEqual(created.ImageUrl, updated.ImageUrl);
            Assert.False(updated.IsPublished);
            Assert.False(File.Exists(firstFile));
            Assert.True(File.Exists(Path.Combine(_uploads, updated.ImageUrl["/media/".Length..])));
            Assert.Equal("Alice Admin", updated.AuthorName);

            await service.DeleteAsync(updated.Id);
            db.Context.ChangeTracker.Clear();
            Assert.False(await db.Context.NewsItems.AnyAsync(item => item.Id == updated.Id));
            Assert.False(File.Exists(Path.Combine(_uploads, updated.ImageUrl["/media/".Length..])));
        }

        [Fact]
        public async Task Update_WithoutANewImage_KeepsTheCurrentFile()
        {
            using var db = new SqliteDb();
            var service = Service(db.Context, userId: 1);
            var created = await service.InsertAsync(ValidInsert("Habitat note"));

            var updated = await service.UpdateAsync(created.Id, new NewsUpdateRequest
            {
                Title = "Habitat note",
                Text = "The boardwalk is open again.",
                PublishedAt = new DateTime(2026, 5, 2, 12, 0, 0, DateTimeKind.Utc),
                IsPublished = true
            });

            Assert.Equal(created.ImageUrl, updated.ImageUrl);
            Assert.True(File.Exists(Path.Combine(_uploads, updated.ImageUrl["/media/".Length..])));
        }

        [Fact]
        public async Task Search_MatchesTitleOrText_InTheDatabase()
        {
            using var db = new SqliteDb();
            var service = Service(db.Context, userId: 1);

            var matches = await service.GetPublishedAsync(new NewsSearchObject
            {
                FTS = "red panda",
                Page = 1,
                PageSize = 10,
                IncludeTotalCount = true
            });

            Assert.Equal(1, matches.TotalCount);
            Assert.Equal("Meet the new red panda twins", matches.Items[0].Title);

            var none = await service.GetAllAsync(new NewsSearchObject
            {
                FTS = "no-such-term",
                Page = 1,
                PageSize = 10,
                IncludeTotalCount = true
            });
            Assert.Equal(0, none.TotalCount);
        }

        [Fact]
        public async Task PageSize_AboveTheMaximum_IsRejected()
        {
            using var db = new SqliteDb();
            var service = Service(db.Context, userId: 1);

            var error = await Assert.ThrowsAsync<ClientException>(() => service.GetPublishedAsync(new NewsSearchObject
            {
                Page = 1,
                PageSize = 101
            }));

            Assert.Equal("PageSize must be between 1 and 100.", error.Message);
        }

        [Fact]
        public async Task Insert_WithoutASignedInUser_IsRejected()
        {
            using var db = new SqliteDb();
            var service = Service(db.Context, userId: null);

            var error = await Assert.ThrowsAsync<ClientException>(() => service.InsertAsync(ValidInsert("Unsigned")));

            Assert.Equal(NewsService.SignInMessage, error.Message);
            Assert.False(Directory.EnumerateFiles(_uploads).Any());
        }

        private NewsService Service(ECommerceDbContext context, int? userId, IStoredImageService? images = null)
        {
            return new NewsService(
                context,
                new Mapper(),
                new NewsInsertValidator(),
                new NewsUpdateValidator(),
                images ?? new FileStoredImageService(new UploadSettings(_uploads)),
                new NewsUserAccessor(userId),
                new SilentLoggerFactory());
        }

        private static NewsInsertRequest ValidInsert(string title)
        {
            return new NewsInsertRequest
            {
                Title = title,
                Text = "A short announcement for visitors.",
                ImageBase64 = Convert.ToBase64String(NewsSeedImages.SolidPng(1, 2, 3)),
                ImageContentType = "image/png",
                PublishedAt = new DateTime(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc),
                IsPublished = true
            };
        }

        private sealed class NewsUserAccessor : IAuthenticatedUserAccessor
        {
            public NewsUserAccessor(int? userId)
            {
                UserId = userId;
            }

            public int? UserId { get; }

            public int? GetUserId() => UserId;

            public bool IsInRole(string role) => false;
        }
    }
}

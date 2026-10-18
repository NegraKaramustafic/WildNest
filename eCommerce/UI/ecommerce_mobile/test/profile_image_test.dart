import 'dart:convert';
import 'dart:math';
import 'dart:typed_data';

import 'package:ecommerce_mobile/utils/profile_image.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:image/image.dart' as img;

void main() {
  test('a small JPEG is kept', () {
    final picture = img.Image(width: 8, height: 8);
    img.fill(picture, color: img.ColorRgb8(30, 120, 70));
    final jpeg = Uint8List.fromList(img.encodeJpg(picture, quality: 80));

    final prepared = prepareProfileImage(jpeg);

    expect(prepared, jpeg);
    expect(prepared!.length, lessThanOrEqualTo(profileImageMaxBytes));
  });

  test('a large picture is compressed under 512 KB as JPEG', () {
    final picture = img.Image(width: 900, height: 900);
    final random = Random(7);
    for (final pixel in picture) {
      pixel
        ..r = random.nextInt(256)
        ..g = random.nextInt(256)
        ..b = random.nextInt(256);
    }
    final original = Uint8List.fromList(img.encodePng(picture));
    expect(original.length, greaterThan(profileImageMaxBytes));

    final prepared = prepareProfileImage(original);

    expect(prepared, isNotNull);
    expect(prepared!.length, lessThanOrEqualTo(profileImageMaxBytes));
    expect(prepared[0], 0xFF);
    expect(prepared[1], 0xD8);
    expect(prepared[2], 0xFF);
  });

  test('bytes that are not a picture are rejected', () {
    expect(
      prepareProfileImage(Uint8List.fromList(const [1, 2, 3, 4, 5, 6])),
      isNull,
    );
    expect(decodeProfileImage(''), isNull);
    expect(decodeProfileImage('not-base64'), isNull);
  });

  test('a data URL is decoded to the picture bytes', () {
    final picture = img.Image(width: 2, height: 2);
    img.fill(picture, color: img.ColorRgb8(1, 2, 3));
    final jpeg = Uint8List.fromList(img.encodeJpg(picture));

    final decoded = decodeProfileImage(
      'data:image/jpeg;base64,${base64Encode(jpeg)}',
    );

    expect(decoded, jpeg);
  });
}

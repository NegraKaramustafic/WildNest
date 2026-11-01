import 'dart:convert';
import 'dart:typed_data';

import 'package:ecommerce_desktop/config/api_config.dart';
import 'package:ecommerce_desktop/providers/auth_provider.dart';
import 'package:ecommerce_desktop/theme/app_colors.dart';
import 'package:ecommerce_desktop/utils/image_bytes.dart';
import 'package:file_selector/file_selector.dart';
import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;

class PickedImage {
  const PickedImage({required this.bytes, required this.contentType, required this.base64});

  final Uint8List bytes;
  final String contentType;
  final String base64;
}

Future<({PickedImage? image, String? error})> pickPngOrJpeg() async {
  const group = XTypeGroup(
    label: 'Images',
    extensions: ['png', 'jpg', 'jpeg'],
    mimeTypes: ['image/png', 'image/jpeg'],
  );
  final file = await openFile(acceptedTypeGroups: [group]);
  if (file == null) {
    return (image: null, error: null);
  }
  final bytes = await file.readAsBytes();
  final type = contentTypeFor(bytes);
  if (type == null) {
    return (image: null, error: imageTooLargeMessage);
  }
  return (
    image: PickedImage(bytes: bytes, contentType: type, base64: base64Encode(bytes)),
    error: null,
  );
}

class ImageField extends StatelessWidget {
  const ImageField({
    super.key,
    this.bytes,
    this.existingUrl,
    this.existingBase64,
    this.error,
    required this.onPick,
    this.onClear,
  });

  final Uint8List? bytes;
  final String? existingUrl;
  final String? existingBase64;
  final String? error;
  final VoidCallback onPick;
  final VoidCallback? onClear;

  @override
  Widget build(BuildContext context) {
    final preview = bytes ?? decodeImageBytes(existingBase64);
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Row(
          children: [
            ClipRRect(
              borderRadius: BorderRadius.circular(12),
              child: SizedBox(
                width: 96,
                height: 96,
                child: preview != null
                    ? Image.memory(preview, fit: BoxFit.cover)
                    : _urlPreview(existingUrl),
              ),
            ),
            const SizedBox(width: 16),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  OutlinedButton.icon(
                    onPressed: onPick,
                    icon: const Icon(Icons.upload_outlined, size: 18),
                    label: const Text('Upload image'),
                  ),
                  if (onClear != null) ...[
                    const SizedBox(height: 8),
                    TextButton(onPressed: onClear, child: const Text('Remove image')),
                  ],
                  const SizedBox(height: 6),
                  const Text(
                    'PNG or JPEG, up to 512 KB.',
                    style: TextStyle(color: AppColors.textMuted, fontSize: 12),
                  ),
                ],
              ),
            ),
          ],
        ),
        if (error != null) ...[
          const SizedBox(height: 6),
          Text(error!, style: const TextStyle(color: AppColors.danger, fontSize: 12)),
        ],
      ],
    );
  }

  Widget _urlPreview(String? url) {
    final image = url?.trim();
    if (image == null || image.isEmpty) {
      return const ColoredBox(
        color: AppColors.border,
        child: Icon(Icons.image_outlined, color: AppColors.textMuted),
      );
    }
    if (image.startsWith('/')) {
      return _AuthorizedPreview(path: image);
    }
    return Image.network(
      image,
      fit: BoxFit.cover,
      errorBuilder: (_, _, _) => const ColoredBox(
        color: AppColors.border,
        child: Icon(Icons.broken_image_outlined, color: AppColors.textMuted),
      ),
    );
  }
}

class _AuthorizedPreview extends StatefulWidget {
  const _AuthorizedPreview({required this.path});

  final String path;

  @override
  State<_AuthorizedPreview> createState() => _AuthorizedPreviewState();
}

class _AuthorizedPreviewState extends State<_AuthorizedPreview> {
  Uint8List? _bytes;
  bool _failed = false;

  @override
  void initState() {
    super.initState();
    _load();
  }

  @override
  void didUpdateWidget(covariant _AuthorizedPreview oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.path != widget.path) {
      _bytes = null;
      _failed = false;
      _load();
    }
  }

  Future<void> _load() async {
    try {
      final token = AuthProvider.accesstoken;
      final headers = <String, String>{};
      if (token != null && token.isNotEmpty) {
        headers['Authorization'] = 'Bearer $token';
      }
      final relative = widget.path.startsWith('/') ? widget.path.substring(1) : widget.path;
      final response = await http.get(Uri.parse('${ApiConfig.baseUrl}$relative'), headers: headers);
      if (!mounted) return;
      if (response.statusCode >= 300 || response.bodyBytes.isEmpty) {
        setState(() => _failed = true);
        return;
      }
      setState(() => _bytes = response.bodyBytes);
    } catch (_) {
      if (mounted) setState(() => _failed = true);
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_bytes != null) {
      return Image.memory(_bytes!, fit: BoxFit.cover, gaplessPlayback: true);
    }
    return ColoredBox(
      color: AppColors.border,
      child: Icon(
        _failed ? Icons.broken_image_outlined : Icons.image_outlined,
        color: AppColors.textMuted,
      ),
    );
  }
}

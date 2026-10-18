import 'dart:typed_data';

import 'package:ecommerce_mobile/config/api_config.dart';
import 'package:ecommerce_mobile/providers/auth_provider.dart';
import 'package:ecommerce_mobile/theme/app_colors.dart';
import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;

class AuthorizedImage extends StatefulWidget {
  const AuthorizedImage({
    super.key,
    required this.url,
    this.fit = BoxFit.cover,
    this.fallbackIcon = Icons.article_outlined,
  });

  final String? url;
  final BoxFit fit;
  final IconData fallbackIcon;

  @override
  State<AuthorizedImage> createState() => _AuthorizedImageState();
}

class _AuthorizedImageState extends State<AuthorizedImage> {
  Uint8List? _bytes;
  bool _failed = false;

  @override
  void initState() {
    super.initState();
    _load();
  }

  @override
  void didUpdateWidget(covariant AuthorizedImage oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.url != widget.url) {
      _bytes = null;
      _failed = false;
      _load();
    }
  }

  Future<void> _load() async {
    final image = widget.url?.trim();
    if (image == null || image.isEmpty) {
      if (mounted) setState(() => _failed = true);
      return;
    }
    try {
      final token = AuthProvider.accesstoken;
      final headers = <String, String>{};
      if (token != null && token.isNotEmpty) {
        headers['Authorization'] = 'Bearer $token';
      }
      final path = image.startsWith('/') ? image.substring(1) : image;
      final target = image.startsWith('http') ? image : '${ApiConfig.baseUrl}$path';
      final response = await http.get(Uri.parse(target), headers: headers);
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
      return Image.memory(_bytes!, fit: widget.fit, gaplessPlayback: true, width: double.infinity, height: double.infinity);
    }
    return ColoredBox(
      color: AppColors.mint,
      child: Icon(
        _failed ? Icons.broken_image_outlined : widget.fallbackIcon,
        color: AppColors.sage,
      ),
    );
  }
}

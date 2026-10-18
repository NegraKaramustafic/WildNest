import 'dart:convert';
import 'dart:math' as math;
import 'dart:typed_data';

import 'package:image/image.dart' as img;

/// Server limit for a stored profile picture. Matches `ImageContent.MaxBytes`.
const int profileImageMaxBytes = 512 * 1024;

const String profileImageInvalidMessage =
    'Image must be a PNG or JPEG file of at most 512 KB.';

Uint8List? decodeProfileImage(String? raw) {
  if (raw == null || raw.trim().isEmpty) return null;
  try {
    final payload = raw.contains(',') ? raw.split(',').last.trim() : raw.trim();
    if (payload.isEmpty) return null;
    return base64Decode(payload);
  } catch (_) {
    return null;
  }
}

/// Returns PNG or JPEG bytes of at most [profileImageMaxBytes].
/// Other formats are re-encoded as JPEG. Returns null when that is not possible.
Uint8List? prepareProfileImage(Uint8List original) {
  if (_isPngOrJpeg(original) && original.length <= profileImageMaxBytes) {
    return Uint8List.fromList(original);
  }

  final decoded = img.decodeImage(original);
  if (decoded == null) return null;

  var edge = math.max(decoded.width, decoded.height);
  var working = decoded;
  if (edge > 1600) {
    working = _resizeToEdge(decoded, 1600);
    edge = 1600;
  }

  var quality = 85;
  var encoded = Uint8List.fromList(img.encodeJpg(working, quality: quality));
  while (encoded.length > profileImageMaxBytes && quality > 45) {
    quality -= 10;
    encoded = Uint8List.fromList(img.encodeJpg(working, quality: quality));
  }

  while (encoded.length > profileImageMaxBytes && edge > 480) {
    edge = (edge * 0.75).round();
    working = _resizeToEdge(decoded, edge);
    encoded = Uint8List.fromList(img.encodeJpg(working, quality: 55));
  }

  if (encoded.length > profileImageMaxBytes || !_isJpeg(encoded)) return null;
  return encoded;
}

img.Image _resizeToEdge(img.Image source, int edge) {
  if (source.width >= source.height) {
    return img.copyResize(source, width: edge);
  }
  return img.copyResize(source, height: edge);
}

bool _isPngOrJpeg(Uint8List bytes) => _isPng(bytes) || _isJpeg(bytes);

bool _isPng(Uint8List bytes) {
  return bytes.length >= 8 &&
      bytes[0] == 0x89 &&
      bytes[1] == 0x50 &&
      bytes[2] == 0x4E &&
      bytes[3] == 0x47 &&
      bytes[4] == 0x0D &&
      bytes[5] == 0x0A &&
      bytes[6] == 0x1A &&
      bytes[7] == 0x0A;
}

bool _isJpeg(Uint8List bytes) {
  return bytes.length >= 3 &&
      bytes[0] == 0xFF &&
      bytes[1] == 0xD8 &&
      bytes[2] == 0xFF;
}

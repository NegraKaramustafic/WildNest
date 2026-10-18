import 'dart:convert';
import 'dart:typed_data';

const imageTooLargeMessage = 'Image must be a PNG or JPEG file of at most 512 KB.';

const maxImageBytes = 512 * 1024;

String? contentTypeFor(Uint8List bytes) {
  if (bytes.length > maxImageBytes) return null;
  if (bytes.length >= 8 &&
      bytes[0] == 0x89 &&
      bytes[1] == 0x50 &&
      bytes[2] == 0x4E &&
      bytes[3] == 0x47 &&
      bytes[4] == 0x0D &&
      bytes[5] == 0x0A &&
      bytes[6] == 0x1A &&
      bytes[7] == 0x0A) {
    return 'image/png';
  }
  if (bytes.length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF) {
    return 'image/jpeg';
  }
  return null;
}

String? imageValidationMessage(Uint8List bytes) {
  return contentTypeFor(bytes) == null ? imageTooLargeMessage : null;
}

Uint8List? decodeImageBytes(String? raw) {
  if (raw == null || raw.trim().isEmpty) return null;
  try {
    final payload = raw.contains(',') ? raw.split(',').last.trim() : raw.trim();
    final bytes = base64Decode(payload);
    if (contentTypeFor(bytes) == null) return null;
    return bytes;
  } catch (_) {
    return null;
  }
}

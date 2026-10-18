import 'dart:typed_data';

import 'package:ecommerce_desktop/utils/pdf_launcher_stub.dart'
    if (dart.library.io) 'package:ecommerce_desktop/utils/pdf_launcher_io.dart' as launcher;

Future<String?> openPdfBytes({
  required String fileName,
  required Uint8List bytes,
  String? savedPath,
}) {
  return launcher.openPdfBytes(fileName: fileName, bytes: bytes, savedPath: savedPath);
}

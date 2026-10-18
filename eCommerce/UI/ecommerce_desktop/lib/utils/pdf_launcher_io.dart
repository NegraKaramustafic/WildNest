import 'dart:io';
import 'dart:typed_data';

Future<String?> openPdfBytes({
  required String fileName,
  required Uint8List bytes,
  String? savedPath,
}) async {
  try {
    final path = (savedPath != null && savedPath.isNotEmpty) ? savedPath : await _writeTemp(fileName, bytes);
    final ProcessResult result;
    if (Platform.isWindows) {
      final escaped = path.replaceAll("'", "''");
      result = await Process.run(
        'powershell',
        ['-NoProfile', '-NonInteractive', '-Command', "Start-Process -FilePath '$escaped'"],
      );
    } else if (Platform.isMacOS) {
      result = await Process.run('open', [path]);
    } else {
      result = await Process.run('xdg-open', [path]);
    }
    if (result.exitCode != 0) {
      return 'The PDF could not be opened.';
    }
    return null;
  } catch (_) {
    return 'The PDF could not be opened.';
  }
}

Future<String> _writeTemp(String fileName, Uint8List bytes) async {
  final directory = await Directory.systemTemp.createTemp('wildnest-report');
  final safeName = fileName.replaceAll(RegExp(r'[<>:"/\\|?*]'), '-');
  final file = File('${directory.path}${Platform.pathSeparator}$safeName');
  await file.writeAsBytes(bytes, flush: true);
  return file.path;
}

import 'dart:typed_data';

import 'package:ecommerce_desktop/providers/base_provider.dart';
import 'package:ecommerce_desktop/utils/api_client_exception.dart';
import 'package:ecommerce_desktop/utils/formatters.dart';
import 'package:http/http.dart' as http;

class ReportProvider extends BaseProvider<void> {
  ReportProvider() : super('api/Report');

  Future<Uint8List> reservationsPdf({
    required DateTime dateFrom,
    required DateTime dateTo,
    int? activityId,
    String? status,
  }) {
    return _download('reservations', {
      'dateFrom': formatApiDate(dateFrom),
      'dateTo': formatApiDate(dateTo),
      'activityId': activityId,
      'status': status,
    });
  }

  Future<Uint8List> donationsPdf({
    required DateTime dateFrom,
    required DateTime dateTo,
    int? zooPartnerId,
  }) {
    return _download('donations', {
      'dateFrom': formatApiDate(dateFrom),
      'dateTo': formatApiDate(dateTo),
      'zooPartnerId': zooPartnerId,
    });
  }

  Future<Uint8List> _download(String action, Map<String, dynamic> query) async {
    final params = <String, String>{};
    query.forEach((key, value) {
      if (value == null) return;
      final text = value.toString();
      if (text.isEmpty) return;
      params[key] = text;
    });
    final response = await http.get(
      Uri.parse('${BaseProvider.baseUrl}$endpoint/$action').replace(queryParameters: params),
      headers: createHeaders(),
    );
    await validateResponse(response);
    final bytes = response.bodyBytes;
    if (bytes.length < 4 ||
        bytes[0] != 0x25 ||
        bytes[1] != 0x50 ||
        bytes[2] != 0x44 ||
        bytes[3] != 0x46) {
      throw ApiClientException('The report file was not a PDF.');
    }
    return bytes;
  }
}

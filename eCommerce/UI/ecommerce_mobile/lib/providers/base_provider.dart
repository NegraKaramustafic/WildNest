import 'dart:convert';

import 'package:ecommerce_mobile/config/api_config.dart';
import 'package:ecommerce_mobile/models/search_result.dart';
import 'package:ecommerce_mobile/providers/auth_provider.dart';
import 'package:ecommerce_mobile/utils/api_client_exception.dart';
import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;
import 'package:http/http.dart';

abstract class BaseProvider<T> with ChangeNotifier {
  /// API resource segment, e.g. `api/Animal` → `{API_BASE_URL}api/Animal`.
  final String endpoint;

  BaseProvider(this.endpoint);

  static String get baseUrl => ApiConfig.baseUrl;

  Future<SearchResult<T>> get({dynamic filter}) async {
    var url = "$baseUrl$endpoint";
    if (filter != null) {
      final queryString = getQueryString(filter);
      if (queryString.isNotEmpty) {
        url = "$url?$queryString";
      }
    }

    final uri = Uri.parse(url);
    final response = await http.get(uri, headers: createHeaders());

    await validateResponse(response);
    final data = jsonDecode(response.body);

    final result = SearchResult<T>();
    result.totalCount = data['totalCount'];
    result.items = List<T>.from((data["items"] as List).map((e) => fromJson(e)));
    return result;
  }

  Future<T> getById(int id) async {
    final url = "$baseUrl$endpoint/$id";
    final uri = Uri.parse(url);
    final response = await http.get(uri, headers: createHeaders());
    await validateResponse(response);
    final data = jsonDecode(response.body);
    return fromJson(data);
  }

  Future<T> insert(dynamic request) async {
    final url = "$baseUrl$endpoint";
    final uri = Uri.parse(url);
    final jsonRequest = jsonEncode(request);
    final response = await http.post(
      uri,
      headers: createHeaders(),
      body: jsonRequest,
    );

    await validateResponse(response);
    final data = jsonDecode(response.body);
    return fromJson(data);
  }

  Future<T> update(int id, [dynamic request]) async {
    final url = "$baseUrl$endpoint/$id";
    final uri = Uri.parse(url);
    final jsonRequest = jsonEncode(request);
    final response = await http.put(
      uri,
      headers: createHeaders(),
      body: jsonRequest,
    );

    await validateResponse(response);
    final data = jsonDecode(response.body);
    return fromJson(data);
  }

  Future<void> remove(int id) async {
    final url = "$baseUrl$endpoint/$id";
    final uri = Uri.parse(url);
    final response = await http.delete(uri, headers: createHeaders());
    await validateResponse(response);
  }

  T fromJson(dynamic data) {
    throw Exception("Method not implemented");
  }

  Future<void> validateResponse(Response response) async {
    if (response.statusCode < 299) {
      return;
    }
    if (response.statusCode == 401) {
      await AuthProvider.instance?.handleUnauthorized();
      throw SessionExpiredException();
    }

    final parsed = ApiErrorParser.messageFromBody(response.body);
    if (response.statusCode >= 500) {
      throw ApiClientException(
        parsed ?? 'Server error. Please try again later.',
      );
    }

    throw ApiClientException(
      parsed ?? 'Request could not be completed. Please try again.',
    );
  }

  Map<String, String> createHeaders() {
    final headers = <String, String>{"Content-Type": "application/json"};
    final token = AuthProvider.accesstoken;
    if (token != null && token.isNotEmpty) {
      headers["Authorization"] = "Bearer $token";
    }
    return headers;
  }

  String getQueryString(
    Map params, {
    String prefix = '',
    bool inRecursion = false,
  }) {
    var query = '';
    params.forEach((key, value) {
      if (value == null) {
        return;
      }
      if (inRecursion) {
        if (key is int) {
          key = '[$key]';
        } else {
          key = '.$key';
        }
      }
      if (value is String || value is int || value is double || value is bool) {
        var encoded = value;
        if (value is String) {
          if (value.isEmpty) {
            return;
          }
          encoded = Uri.encodeComponent(value);
        }
        final separator = query.isEmpty && prefix.isEmpty ? '' : '&';
        query += '$separator$prefix$key=$encoded';
      } else if (value is DateTime) {
        final separator = query.isEmpty && prefix.isEmpty ? '' : '&';
        query += '$separator$prefix$key=${value.toIso8601String()}';
      } else if (value is List || value is Map) {
        if (value is List) {
          value = value.asMap();
        }
        value.forEach((k, v) {
          query += getQueryString(
            {k: v},
            prefix: '$prefix$key',
            inRecursion: true,
          );
        });
      }
    });
    return query.startsWith('&') ? query.substring(1) : query;
  }
}

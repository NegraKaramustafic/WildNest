import 'dart:convert';

import 'package:ecommerce_desktop/config/api_config.dart';
import 'package:ecommerce_desktop/models/search_result.dart';
import 'package:ecommerce_desktop/providers/auth_provider.dart';
import 'package:ecommerce_desktop/utils/api_client_exception.dart';
import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;
import 'package:http/http.dart';

abstract class BaseProvider<T> with ChangeNotifier {
  final String endpoint;

  BaseProvider(this.endpoint);

  static String get baseUrl => ApiConfig.baseUrl;

  Future<SearchResult<T>> get({dynamic filter}) async {
    var url = '$baseUrl$endpoint';
    if (filter != null) {
      final queryString = getQueryString(filter);
      if (queryString.isNotEmpty) {
        url = '$url?$queryString';
      }
    }

    final response = await http.get(Uri.parse(url), headers: createHeaders());
    await validateResponse(response);
    final data = jsonDecode(response.body);

    final result = SearchResult<T>();
    result.totalCount = data['totalCount'];
    result.items = List<T>.from((data['items'] as List).map((e) => fromJson(e)));
    return result;
  }

  Future<T> insert(dynamic request) async {
    final response = await http.post(
      Uri.parse('$baseUrl$endpoint'),
      headers: createHeaders(),
      body: jsonEncode(request),
    );
    await validateResponse(response);
    return fromJson(jsonDecode(response.body));
  }

  Future<T> update(int id, [dynamic request]) async {
    final response = await http.put(
      Uri.parse('$baseUrl$endpoint/$id'),
      headers: createHeaders(),
      body: jsonEncode(request),
    );
    await validateResponse(response);
    return fromJson(jsonDecode(response.body));
  }

  Future<void> remove(int id) async {
    final response = await http.delete(
      Uri.parse('$baseUrl$endpoint/$id'),
      headers: createHeaders(),
    );
    await validateResponse(response);
  }

  T fromJson(dynamic data) {
    throw Exception('Method not implemented');
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
    if (response.statusCode == 404) {
      throw ApiClientException(parsed ?? 'Record not found.');
    }
    if (response.statusCode >= 500) {
      throw ApiClientException(parsed ?? 'Server error. Please try again.');
    }
    throw ApiClientException(
      parsed ?? 'The request could not be completed. Please try again.',
    );
  }

  Map<String, String> createHeaders() {
    final headers = <String, String>{'Content-Type': 'application/json'};
    final token = AuthProvider.accesstoken;
    if (token != null && token.isNotEmpty) {
      headers['Authorization'] = 'Bearer $token';
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
      var currentKey = key;
      if (inRecursion) {
        currentKey = key is int ? '[$key]' : '.$key';
      }
      if (value is String || value is int || value is double || value is bool) {
        if (value is String && value.isEmpty) {
          return;
        }
        final encoded = value is String ? Uri.encodeComponent(value) : value;
        final separator = query.isEmpty && prefix.isEmpty ? '' : '&';
        query += '$separator$prefix$currentKey=$encoded';
      } else if (value is DateTime) {
        final separator = query.isEmpty && prefix.isEmpty ? '' : '&';
        query += '$separator$prefix$currentKey=${value.toIso8601String()}';
      } else if (value is List || value is Map) {
        final nested = value is List ? value.asMap() : value;
        nested.forEach((k, v) {
          query += getQueryString(
            {k: v},
            prefix: '$prefix$currentKey',
            inRecursion: true,
          );
        });
      }
    });
    return query.startsWith('&') ? query.substring(1) : query;
  }
}

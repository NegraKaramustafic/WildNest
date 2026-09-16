import 'dart:async';
import 'dart:convert';

import 'package:ecommerce_mobile/models/app_notification.dart';
import 'package:ecommerce_mobile/providers/base_provider.dart';
import 'package:flutter/widgets.dart';
import 'package:http/http.dart' as http;

class NotificationProvider extends BaseProvider<AppNotification> {
  static const _pollInterval = Duration(seconds: 12);

  NotificationProvider() : super('api/Notification');

  Timer? _timer;
  List<AppNotification> _items = [];
  int _unreadCount = 0;
  bool _loading = false;
  String? _error;

  List<AppNotification> get items => List.unmodifiable(_items);
  int get unreadCount => _unreadCount;
  bool get hasUnread => _unreadCount > 0;
  bool get loading => _loading;
  String? get error => _error;

  @override
  AppNotification fromJson(data) {
    return AppNotification.fromJson(data as Map<String, dynamic>);
  }

  void startPolling() {
    _timer?.cancel();
    _timer = Timer.periodic(_pollInterval, (_) => refresh());
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (_timer == null) return;
      refresh();
    });
  }

  void stopPolling() {
    _timer?.cancel();
    _timer = null;
  }

  @override
  void dispose() {
    stopPolling();
    super.dispose();
  }

  Future<void> refresh() async {
    _loading = _items.isEmpty;
    notifyListeners();
    try {
      final page = await get(
        filter: {
          'page': 1,
          'pageSize': 50,
          'includeTotalCount': false,
          'SortBy': 'CreatedAt desc',
        },
      );
      final count = await getUnreadCount();
      _items = page.items ?? [];
      _unreadCount = count;
      _error = null;
    } catch (e) {
      _error = e.toString();
    } finally {
      _loading = false;
      notifyListeners();
    }
  }

  Future<int> getUnreadCount() async {
    final uri = Uri.parse('${BaseProvider.baseUrl}$endpoint/unread-count');
    final response = await http.get(uri, headers: createHeaders());
    await validateResponse(response);
    final data = jsonDecode(response.body);
    if (data is int) return data;
    if (data is num) return data.toInt();
    return int.tryParse(data.toString()) ?? 0;
  }

  Future<void> markRead(int id) async {
    try {
      final uri = Uri.parse('${BaseProvider.baseUrl}$endpoint/$id/read');
      final response = await http.put(uri, headers: createHeaders());
      await validateResponse(response);
      final updated = fromJson(jsonDecode(response.body));
      _items = _items
          .map((item) => item.id == id ? updated : item)
          .toList(growable: false);
      _unreadCount = _items.where((item) => !item.isRead).length;
      notifyListeners();
    } catch (_) {
      await refresh();
    }
  }
}

import 'dart:async';

import 'package:ecommerce_desktop/models/activity.dart';
import 'package:ecommerce_desktop/models/payment_status.dart';
import 'package:ecommerce_desktop/models/reservation.dart';
import 'package:ecommerce_desktop/providers/activity_provider.dart';
import 'package:ecommerce_desktop/providers/reservation_provider.dart';
import 'package:ecommerce_desktop/theme/app_colors.dart';
import 'package:ecommerce_desktop/utils/api_client_exception.dart';
import 'package:ecommerce_desktop/utils/formatters.dart';
import 'package:ecommerce_desktop/widgets/admin_list.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

class ReservationsScreen extends StatefulWidget {
  const ReservationsScreen({super.key});

  @override
  State<ReservationsScreen> createState() => _ReservationsScreenState();
}

class _ReservationsScreenState extends State<ReservationsScreen> {
  static const _pageSize = 10;

  final _searchController = TextEditingController();
  Timer? _debounce;
  int _requestId = 0;
  int _page = 1;
  int _total = 0;
  int? _activityId;
  ReservationStatus? _status;
  DateTime? _dateFrom;
  DateTime? _dateTo;
  bool _loading = true;
  bool _loadingActivities = true;
  String? _error;
  List<Reservation> _items = [];
  List<Activity> _activities = [];

  @override
  void initState() {
    super.initState();
    _loadActivities();
    _load();
  }

  @override
  void dispose() {
    _debounce?.cancel();
    _searchController.dispose();
    super.dispose();
  }

  Future<void> _loadActivities() async {
    try {
      final result = await context.read<ActivityProvider>().get(
        filter: const {'Page': 1, 'PageSize': 100},
      );
      if (!mounted) return;
      setState(() {
        _activities = result.items ?? [];
        _loadingActivities = false;
      });
    } on SessionExpiredException {
      return;
    } catch (_) {
      if (!mounted) return;
      setState(() => _loadingActivities = false);
    }
  }

  Future<void> _load() async {
    if (_dateFrom != null &&
        _dateTo != null &&
        DateUtils.dateOnly(_dateFrom!).isAfter(DateUtils.dateOnly(_dateTo!))) {
      setState(() {
        _loading = false;
        _error = 'From date cannot be after to date.';
        _items = [];
        _total = 0;
      });
      return;
    }

    final requestId = ++_requestId;
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final result = await context.read<ReservationProvider>().get(
        filter: {
          'FTS': _searchController.text.trim(),
          'Status': _status == null ? null : reservationStatusApiValue(_status!),
          'ActivityId': _activityId,
          'DateFrom': _dateFrom == null ? null : formatApiDate(_dateFrom!),
          'DateTo': _dateTo == null ? null : formatApiDate(_dateTo!),
          'Page': _page,
          'PageSize': _pageSize,
          'IncludeTotalCount': true,
        },
      );
      if (!mounted || requestId != _requestId) return;
      final items = result.items ?? [];
      final total = result.totalCount ?? items.length;
      if (items.isEmpty && total > 0 && _page > 1) {
        setState(() => _page -= 1);
        await _load();
        return;
      }
      setState(() {
        _items = items;
        _total = total;
        _loading = false;
      });
    } on SessionExpiredException {
      return;
    } catch (error) {
      if (!mounted || requestId != _requestId) return;
      setState(() {
        _error = friendlyError(error);
        _loading = false;
      });
    }
  }

  void _onSearchChanged(String value) {
    _debounce?.cancel();
    _debounce = Timer(const Duration(milliseconds: 300), () {
      if (!mounted) return;
      setState(() => _page = 1);
      _load();
    });
  }

  void _apply(VoidCallback change) {
    setState(() {
      change();
      _page = 1;
    });
    _load();
  }

  Future<void> _pickDate({required bool from}) async {
    final current = from ? _dateFrom : _dateTo;
    final today = DateTime.now();
    final picked = await showDatePicker(
      context: context,
      initialDate: current ?? DateTime(today.year, today.month, today.day),
      firstDate: DateTime(today.year - 2),
      lastDate: DateTime(today.year + 3, 12, 31),
    );
    if (picked == null || !mounted) return;
    final day = DateTime(picked.year, picked.month, picked.day);
    _apply(() {
      if (from) {
        _dateFrom = day;
      } else {
        _dateTo = day;
      }
    });
  }

  Future<void> _confirm(Reservation reservation) async {
    final confirmed = await confirmAdminAction(
      context: context,
      title: 'Confirm reservation',
      message: 'Confirm reservation ${reservation.code} for ${reservation.userFullName}?',
      confirmLabel: 'Confirm',
    );
    if (!confirmed || !mounted) return;
    await _run(() => context.read<ReservationProvider>().confirm(reservation.id));
  }

  Future<void> _complete(Reservation reservation) async {
    final confirmed = await confirmAdminAction(
      context: context,
      title: 'Complete reservation',
      message: 'Mark reservation ${reservation.code} as completed?',
      confirmLabel: 'Mark as completed',
    );
    if (!confirmed || !mounted) return;
    await _run(() => context.read<ReservationProvider>().complete(reservation.id));
  }

  Future<void> _cancel(Reservation reservation) async {
    final reason = await askForReason(
      context: context,
      title: 'Cancel reservation',
      message: 'Cancel reservation ${reservation.code}? A reason is required.',
      confirmLabel: 'Cancel reservation',
    );
    if (reason == null || !mounted) return;
    await _run(() => context.read<ReservationProvider>().cancelWithReason(reservation.id, reason));
  }

  Future<void> _run(Future<void> Function() action) async {
    try {
      await action();
      if (mounted) await _load();
    } on SessionExpiredException {
      return;
    } catch (error) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(friendlyError(error))));
    }
  }

  Future<void> _details(Reservation reservation) async {
    await showDialog<void>(
      context: context,
      builder: (context) => AlertDialog(
        title: Text(reservation.code),
        content: SizedBox(
          width: 480,
          child: SingleChildScrollView(
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                DetailLine(label: 'Customer', value: _text(reservation.userFullName)),
                DetailLine(label: 'Activity', value: _text(reservation.activityName)),
                DetailLine(
                  label: 'Schedule',
                  value: '${formatDate(reservation.scheduleDate)}, ${formatClock(reservation.scheduleStartTime)}',
                ),
                DetailLine(label: 'Guests', value: formatCount(reservation.guestCount)),
                DetailLine(label: 'Total', value: formatKm(reservation.totalPrice)),
                DetailLine(label: 'Status', value: reservationStatusLabel(reservation.status)),
                DetailLine(label: 'Payment', value: paymentLabel(reservation)),
                DetailLine(label: 'Created', value: formatDateTime(reservation.createdAt)),
                DetailLine(label: 'Confirmed by', value: _decision(reservation.confirmedByFullName, reservation.confirmedAt)),
                DetailLine(label: 'Cancelled by', value: _decision(reservation.cancelledByFullName, reservation.cancelledAt)),
                DetailLine(label: 'Cancel reason', value: _text(reservation.cancellationReason)),
              ],
            ),
          ),
        ),
        actions: [
          TextButton(onPressed: () => Navigator.pop(context), child: const Text('Close')),
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return AdminScreenFrame(
      title: 'Reservations',
      subtitle: 'Confirm or cancel bookings.',
      filters: Wrap(
        spacing: 12,
        runSpacing: 12,
        crossAxisAlignment: WrapCrossAlignment.center,
        children: [
          SizedBox(
            width: 280,
            child: TextField(
              controller: _searchController,
              onChanged: _onSearchChanged,
              decoration: const InputDecoration(
                hintText: 'Search by code, customer, or activity...',
                prefixIcon: Icon(Icons.search),
                isDense: true,
              ),
            ),
          ),
          SizedBox(width: 200, child: _activityFilter()),
          SizedBox(width: 180, child: _statusFilter()),
          DateFilterField(
            label: 'From date',
            value: _dateFrom,
            onPick: () => _pickDate(from: true),
            onClear: _dateFrom == null ? null : () => _apply(() => _dateFrom = null),
          ),
          DateFilterField(
            label: 'To date',
            value: _dateTo,
            onPick: () => _pickDate(from: false),
            onClear: _dateTo == null ? null : () => _apply(() => _dateTo = null),
          ),
        ],
      ),
      body: _body(),
    );
  }

  Widget _body() {
    if (_loading) return const Center(child: CircularProgressIndicator());
    if (_error != null) return ListError(message: _error!, onRetry: _load);
    return AdminTableCard(
      empty: 'No reservations to show.',
      header: const _Header(),
      pager: ListPager(page: _page, pageSize: _pageSize, total: _total, onPage: (page) {
        setState(() => _page = page);
        _load();
      }),
      rows: [
        for (final reservation in _items)
          _Row(
            reservation: reservation,
            onDetails: () => _details(reservation),
            onConfirm: () => _confirm(reservation),
            onComplete: () => _complete(reservation),
            onCancel: () => _cancel(reservation),
          ),
      ],
    );
  }

  Widget _activityFilter() {
    if (_loadingActivities) {
      return const InputDecorator(
        decoration: InputDecoration(labelText: 'Activity', isDense: true),
        child: Text('Loading...'),
      );
    }
    final selected = _activities.any((activity) => activity.id == _activityId) ? _activityId : null;
    return DropdownButtonFormField<int?>(
      key: ValueKey('reservation-activity-$selected-${_activities.length}'),
      initialValue: selected,
      isExpanded: true,
      decoration: const InputDecoration(labelText: 'Activity', isDense: true),
      items: [
        const DropdownMenuItem(value: null, child: Text('All activities', overflow: TextOverflow.ellipsis)),
        for (final activity in _activities)
          DropdownMenuItem(
            value: activity.id,
            child: Text(activity.name, overflow: TextOverflow.ellipsis),
          ),
      ],
      onChanged: (value) => _apply(() => _activityId = value),
    );
  }

  Widget _statusFilter() {
    return DropdownButtonFormField<ReservationStatus?>(
      key: ValueKey('reservation-status-$_status'),
      initialValue: _status,
      isExpanded: true,
      decoration: const InputDecoration(labelText: 'Status', isDense: true),
      items: [
        const DropdownMenuItem(value: null, child: Text('All statuses', overflow: TextOverflow.ellipsis)),
        for (final status in ReservationStatus.values)
          DropdownMenuItem(
            value: status,
            child: Text(reservationStatusLabel(status), overflow: TextOverflow.ellipsis),
          ),
      ],
      onChanged: (value) => _apply(() => _status = value),
    );
  }
}

class _Header extends StatelessWidget {
  const _Header();

  @override
  Widget build(BuildContext context) {
    const style = TextStyle(fontSize: 12, fontWeight: FontWeight.w700, color: AppColors.textMuted);
    return const Padding(
      padding: EdgeInsets.symmetric(horizontal: 16, vertical: 12),
      child: Row(
        children: [
          SizedBox(width: 42),
          SizedBox(width: 12),
          Expanded(flex: 3, child: Text('Reservation', style: style)),
          Expanded(flex: 2, child: Text('Customer', style: style)),
          Expanded(flex: 2, child: Text('Schedule', style: style)),
          Expanded(flex: 2, child: Text('Total', style: style)),
          Expanded(flex: 2, child: Text('Status', style: style)),
          SizedBox(width: 160, child: Text('Actions', style: style)),
        ],
      ),
    );
  }
}

class _Row extends StatelessWidget {
  const _Row({
    required this.reservation,
    required this.onDetails,
    required this.onConfirm,
    required this.onComplete,
    required this.onCancel,
  });

  final Reservation reservation;
  final VoidCallback onDetails;
  final VoidCallback onConfirm;
  final VoidCallback onComplete;
  final VoidCallback onCancel;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
      child: Row(
        children: [
          EntityThumbnail(url: reservation.activityImageUrl),
          const SizedBox(width: 12),
          Expanded(
            flex: 3,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                EllipsisText(reservation.code, style: const TextStyle(fontWeight: FontWeight.w600)),
                EllipsisText(reservation.activityName, style: const TextStyle(color: AppColors.textMuted, fontSize: 12)),
              ],
            ),
          ),
          Expanded(flex: 2, child: EllipsisText(_text(reservation.userFullName))),
          Expanded(
            flex: 2,
            child: EllipsisText(
              '${formatDate(reservation.scheduleDate)}, ${formatClock(reservation.scheduleStartTime)}',
            ),
          ),
          Expanded(flex: 2, child: EllipsisText(formatKm(reservation.totalPrice))),
          Expanded(flex: 2, child: Align(alignment: Alignment.centerLeft, child: _chip(reservation.status))),
          SizedBox(
            width: 160,
            child: Row(
              children: [
                IconActionButton(
                  icon: Icons.visibility_outlined,
                  color: AppColors.text,
                  tooltip: 'View details',
                  enabled: true,
                  onPressed: onDetails,
                ),
                const SizedBox(width: 6),
                IconActionButton(
                  icon: Icons.check,
                  color: AppColors.green,
                  tooltip: reservation.confirmTooltip,
                  enabled: reservation.canConfirm,
                  onPressed: onConfirm,
                ),
                const SizedBox(width: 6),
                IconActionButton(
                  icon: Icons.task_alt,
                  color: AppColors.greenDark,
                  tooltip: reservation.completeTooltip,
                  enabled: reservation.canComplete,
                  onPressed: onComplete,
                ),
                const SizedBox(width: 6),
                IconActionButton(
                  icon: Icons.close,
                  color: AppColors.danger,
                  tooltip: reservation.cancelTooltip,
                  enabled: reservation.canCancel,
                  onPressed: onCancel,
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

Widget _chip(ReservationStatus status) {
  switch (status) {
    case ReservationStatus.pending:
      return labeledStatusChip(label: 'Pending', tone: 'warning');
    case ReservationStatus.confirmed:
      return labeledStatusChip(label: 'Confirmed', tone: 'success');
    case ReservationStatus.completed:
      return labeledStatusChip(label: 'Completed', tone: 'neutral');
    case ReservationStatus.cancelled:
      return labeledStatusChip(label: 'Cancelled', tone: 'danger');
  }
}

String _text(String? value) {
  final text = value?.trim() ?? '';
  return text.isEmpty ? '—' : text;
}

String _decision(String? name, DateTime? at) {
  if (at == null && (name == null || name.trim().isEmpty)) return '—';
  final who = _text(name);
  if (at == null) return who;
  return '$who, ${formatDateTime(at)}';
}

String paymentLabel(Reservation reservation) {
  return paymentStatusLabel(reservation.paymentStatus);
}

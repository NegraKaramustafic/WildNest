import 'dart:async';

import 'package:ecommerce_desktop/models/adoption.dart';
import 'package:ecommerce_desktop/models/payment_status.dart';
import 'package:ecommerce_desktop/providers/adoption_provider.dart';
import 'package:ecommerce_desktop/theme/app_colors.dart';
import 'package:ecommerce_desktop/utils/api_client_exception.dart';
import 'package:ecommerce_desktop/utils/formatters.dart';
import 'package:ecommerce_desktop/widgets/admin_list.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

class AdoptionsScreen extends StatefulWidget {
  const AdoptionsScreen({super.key});

  @override
  State<AdoptionsScreen> createState() => _AdoptionsScreenState();
}

class _AdoptionsScreenState extends State<AdoptionsScreen> {
  static const _pageSize = 10;

  final _searchController = TextEditingController();
  Timer? _debounce;
  int _requestId = 0;
  int _page = 1;
  int _total = 0;
  AdoptionStatus? _status;
  DateTime? _dateFrom;
  DateTime? _dateTo;
  bool _loading = true;
  String? _error;
  List<Adoption> _items = [];

  @override
  void initState() {
    super.initState();
    _load();
  }

  @override
  void dispose() {
    _debounce?.cancel();
    _searchController.dispose();
    super.dispose();
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
      final result = await context.read<AdoptionProvider>().get(
        filter: {
          'FTS': _searchController.text.trim(),
          'Status': _status == null ? null : adoptionStatusApiValue(_status!),
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

  Future<void> _reject(Adoption adoption) async {
    final reason = await askForReason(
      context: context,
      title: 'Reject adoption',
      message: 'Reject ${adoption.userFullName}\'s adoption of ${adoption.animalName}? A reason is required.',
      confirmLabel: 'Reject',
    );
    if (reason == null || !mounted) return;
    try {
      await context.read<AdoptionProvider>().reject(adoption.id, reason);
      if (mounted) await _load();
    } on SessionExpiredException {
      return;
    } catch (error) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(friendlyError(error))));
    }
  }

  Future<void> _details(Adoption adoption) async {
    final decided = adoption.rejectedAt == null && (adoption.rejectedByFullName ?? '').trim().isEmpty
        ? '—'
        : '${_text(adoption.rejectedByFullName)}, ${formatDateTime(adoption.rejectedAt)}';
    await showDialog<void>(
      context: context,
      builder: (context) => AlertDialog(
        title: Text(_text(adoption.animalName)),
        content: SizedBox(
          width: 480,
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              DetailLine(label: 'Customer', value: _text(adoption.userFullName)),
              DetailLine(label: 'Animal', value: _text(adoption.animalName)),
              DetailLine(label: 'Monthly amount', value: formatMoney(adoption.monthlyAmount)),
              DetailLine(label: 'Status', value: adoptionStatusLabel(adoption.status)),
              DetailLine(label: 'Payment', value: paymentStatusLabel(adoption.paymentStatus)),
              DetailLine(label: 'Started', value: formatDateTime(adoption.startDate)),
              DetailLine(label: 'Rejected by', value: decided),
              DetailLine(label: 'Reason', value: _text(adoption.rejectionReason)),
            ],
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
      title: 'Adoptions',
      subtitle: 'Review virtual adoptions.',
      filters: Wrap(
        spacing: 12,
        runSpacing: 12,
        crossAxisAlignment: WrapCrossAlignment.center,
        children: [
          SizedBox(
            width: 280,
            child: TextField(
              controller: _searchController,
              onChanged: (value) {
                _debounce?.cancel();
                _debounce = Timer(const Duration(milliseconds: 300), () {
                  if (!mounted) return;
                  setState(() => _page = 1);
                  _load();
                });
              },
              decoration: const InputDecoration(
                hintText: 'Search by customer or animal...',
                prefixIcon: Icon(Icons.search),
                isDense: true,
              ),
            ),
          ),
          SizedBox(
            width: 200,
            child: DropdownButtonFormField<AdoptionStatus?>(
              key: ValueKey('adoption-status-$_status'),
              initialValue: _status,
              isExpanded: true,
              decoration: const InputDecoration(labelText: 'Status', isDense: true),
              items: [
                const DropdownMenuItem(value: null, child: Text('All statuses', overflow: TextOverflow.ellipsis)),
                for (final status in AdoptionStatus.values)
                  DropdownMenuItem(
                    value: status,
                    child: Text(adoptionStatusLabel(status), overflow: TextOverflow.ellipsis),
                  ),
              ],
              onChanged: (value) => _apply(() => _status = value),
            ),
          ),
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
    const headerStyle = TextStyle(fontSize: 12, fontWeight: FontWeight.w700, color: AppColors.textMuted);
    return AdminTableCard(
      empty: 'No adoptions to show.',
      pager: ListPager(
        page: _page,
        pageSize: _pageSize,
        total: _total,
        onPage: (page) {
          setState(() => _page = page);
          _load();
        },
      ),
      header: const Padding(
        padding: EdgeInsets.symmetric(horizontal: 16, vertical: 12),
        child: Row(
          children: [
            SizedBox(width: 54),
            Expanded(flex: 3, child: Text('Animal', style: headerStyle)),
            Expanded(flex: 2, child: Text('Customer', style: headerStyle)),
            Expanded(flex: 2, child: Text('Amount', style: headerStyle)),
            Expanded(flex: 2, child: Text('Started', style: headerStyle)),
            Expanded(flex: 2, child: Text('Status', style: headerStyle)),
            SizedBox(width: 120, child: Text('Actions', style: headerStyle)),
          ],
        ),
      ),
      rows: [
        for (final adoption in _items)
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
            child: Row(
              children: [
                EntityThumbnail(url: adoption.animalImageUrl),
                const SizedBox(width: 12),
                Expanded(flex: 3, child: EllipsisText(adoption.animalName, style: const TextStyle(fontWeight: FontWeight.w600))),
                Expanded(flex: 2, child: EllipsisText(_text(adoption.userFullName))),
                Expanded(flex: 2, child: EllipsisText('${formatMoney(adoption.monthlyAmount)} / month')),
                Expanded(flex: 2, child: EllipsisText(formatDateTime(adoption.startDate))),
                Expanded(flex: 2, child: Align(alignment: Alignment.centerLeft, child: _adoptionChip(adoption.status))),
                SizedBox(
                  width: 120,
                  child: Row(
                    children: [
                      IconActionButton(
                        icon: Icons.visibility_outlined,
                        color: AppColors.text,
                        tooltip: 'View details',
                        enabled: true,
                        onPressed: () => _details(adoption),
                      ),
                      const SizedBox(width: 6),
                      IconActionButton(
                        icon: Icons.check,
                        color: AppColors.green,
                        tooltip: adoption.approveTooltip,
                        enabled: false,
                        onPressed: () {},
                      ),
                      const SizedBox(width: 6),
                      IconActionButton(
                        icon: Icons.close,
                        color: AppColors.danger,
                        tooltip: adoption.rejectTooltip,
                        enabled: adoption.canReject,
                        onPressed: () => _reject(adoption),
                      ),
                    ],
                  ),
                ),
              ],
            ),
          ),
      ],
    );
  }
}

Widget _adoptionChip(AdoptionStatus status) {
  switch (status) {
    case AdoptionStatus.pendingPayment:
      return labeledStatusChip(label: 'Pending payment', tone: 'warning');
    case AdoptionStatus.active:
      return labeledStatusChip(label: 'Active', tone: 'success');
    case AdoptionStatus.cancelled:
      return labeledStatusChip(label: 'Cancelled', tone: 'danger');
  }
}

String _text(String? value) {
  final text = value?.trim() ?? '';
  return text.isEmpty ? '—' : text;
}

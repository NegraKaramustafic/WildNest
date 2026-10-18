import 'dart:async';

import 'package:ecommerce_desktop/models/donation.dart';
import 'package:ecommerce_desktop/models/payment_status.dart';
import 'package:ecommerce_desktop/models/zoo_partner.dart';
import 'package:ecommerce_desktop/providers/donation_provider.dart';
import 'package:ecommerce_desktop/providers/zoo_partner_provider.dart';
import 'package:ecommerce_desktop/theme/app_colors.dart';
import 'package:ecommerce_desktop/utils/api_client_exception.dart';
import 'package:ecommerce_desktop/utils/formatters.dart';
import 'package:ecommerce_desktop/widgets/admin_list.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

class DonationsScreen extends StatefulWidget {
  const DonationsScreen({super.key});

  @override
  State<DonationsScreen> createState() => _DonationsScreenState();
}

class _DonationsScreenState extends State<DonationsScreen> {
  static const _pageSize = 10;

  final _searchController = TextEditingController();
  Timer? _debounce;
  int _requestId = 0;
  int _page = 1;
  int _total = 0;
  int? _partnerId;
  String? _paymentFilter;
  DateTime? _dateFrom;
  DateTime? _dateTo;
  bool _loading = true;
  bool _loadingPartners = true;
  String? _error;
  List<Donation> _items = [];
  List<ZooPartner> _partners = [];

  @override
  void initState() {
    super.initState();
    _loadPartners();
    _load();
  }

  @override
  void dispose() {
    _debounce?.cancel();
    _searchController.dispose();
    super.dispose();
  }

  Future<void> _loadPartners() async {
    try {
      final result = await context.read<ZooPartnerProvider>().get(
        filter: const {'Page': 1, 'PageSize': 100},
      );
      if (!mounted) return;
      setState(() {
        _partners = result.items ?? [];
        _loadingPartners = false;
      });
    } on SessionExpiredException {
      return;
    } catch (_) {
      if (!mounted) return;
      setState(() => _loadingPartners = false);
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
      final result = await context.read<DonationProvider>().get(
        filter: {
          'FTS': _searchController.text.trim(),
          'ZooPartnerId': _partnerId,
          'PaymentStatus': _paymentStatusQuery(),
          'HasPayment': _paymentFilter == 'none' ? false : null,
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

  String? _paymentStatusQuery() {
    switch (_paymentFilter) {
      case 'pending':
        return paymentStatusApiValue(PaymentStatus.pending);
      case 'completed':
        return paymentStatusApiValue(PaymentStatus.completed);
      case 'failed':
        return paymentStatusApiValue(PaymentStatus.failed);
      case 'refunded':
        return paymentStatusApiValue(PaymentStatus.refunded);
      default:
        return null;
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

  Future<void> _details(Donation donation) async {
    await showDialog<void>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Donation'),
        content: SizedBox(
          width: 480,
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              DetailLine(label: 'Customer', value: _text(donation.userFullName)),
              DetailLine(label: 'Zoo partner', value: _text(donation.zooPartnerName)),
              DetailLine(label: 'Amount', value: formatKm(donation.amount)),
              DetailLine(label: 'Payment', value: paymentStatusLabel(donation.paymentStatus)),
              DetailLine(label: 'Created', value: formatDateTime(donation.createdAt)),
              const SizedBox(height: 4),
              const Text(
                'Amounts cannot be changed.',
                style: TextStyle(color: AppColors.textMuted),
              ),
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
      title: 'Donations',
      subtitle: 'Review donations and payment status. Amounts cannot be changed.',
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
                hintText: 'Search by customer or partner...',
                prefixIcon: Icon(Icons.search),
                isDense: true,
              ),
            ),
          ),
          SizedBox(width: 200, child: _partnerFilter()),
          SizedBox(width: 180, child: _paymentFilterField()),
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
      empty: 'No donations to show.',
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
            Expanded(flex: 3, child: Text('Zoo partner', style: headerStyle)),
            Expanded(flex: 2, child: Text('Customer', style: headerStyle)),
            Expanded(flex: 2, child: Text('Amount', style: headerStyle)),
            Expanded(flex: 2, child: Text('Created', style: headerStyle)),
            Expanded(flex: 2, child: Text('Payment', style: headerStyle)),
            SizedBox(width: 48, child: Text('Actions', style: headerStyle)),
          ],
        ),
      ),
      rows: [
        for (final donation in _items)
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
            child: Row(
              children: [
                EntityThumbnail(url: donation.zooPartnerImageUrl),
                const SizedBox(width: 12),
                Expanded(
                  flex: 3,
                  child: EllipsisText(donation.zooPartnerName, style: const TextStyle(fontWeight: FontWeight.w600)),
                ),
                Expanded(flex: 2, child: EllipsisText(_text(donation.userFullName))),
                Expanded(flex: 2, child: EllipsisText(formatKm(donation.amount))),
                Expanded(flex: 2, child: EllipsisText(formatDateTime(donation.createdAt))),
                Expanded(flex: 2, child: Align(alignment: Alignment.centerLeft, child: _paymentChip(donation.paymentStatus))),
                SizedBox(
                  width: 48,
                  child: IconActionButton(
                    icon: Icons.visibility_outlined,
                    color: AppColors.text,
                    tooltip: 'View details',
                    enabled: true,
                    onPressed: () => _details(donation),
                  ),
                ),
              ],
            ),
          ),
      ],
    );
  }

  Widget _partnerFilter() {
    if (_loadingPartners) {
      return const InputDecorator(
        decoration: InputDecoration(labelText: 'Partner', isDense: true),
        child: Text('Loading...'),
      );
    }
    final selected = _partners.any((partner) => partner.id == _partnerId) ? _partnerId : null;
    return DropdownButtonFormField<int?>(
      key: ValueKey('donation-partner-$selected-${_partners.length}'),
      initialValue: selected,
      isExpanded: true,
      decoration: const InputDecoration(labelText: 'Partner', isDense: true),
      items: [
        const DropdownMenuItem(value: null, child: Text('All partners', overflow: TextOverflow.ellipsis)),
        for (final partner in _partners)
          DropdownMenuItem(
            value: partner.id,
            child: Text(partner.name, overflow: TextOverflow.ellipsis),
          ),
      ],
      onChanged: (value) => _apply(() => _partnerId = value),
    );
  }

  Widget _paymentFilterField() {
    return DropdownButtonFormField<String?>(
      key: ValueKey('donation-payment-$_paymentFilter'),
      initialValue: _paymentFilter,
      isExpanded: true,
      decoration: const InputDecoration(labelText: 'Payment', isDense: true),
      items: const [
        DropdownMenuItem(value: null, child: Text('All payments', overflow: TextOverflow.ellipsis)),
        DropdownMenuItem(value: 'none', child: Text('No payment', overflow: TextOverflow.ellipsis)),
        DropdownMenuItem(value: 'pending', child: Text('Pending', overflow: TextOverflow.ellipsis)),
        DropdownMenuItem(value: 'completed', child: Text('Completed', overflow: TextOverflow.ellipsis)),
        DropdownMenuItem(value: 'failed', child: Text('Failed', overflow: TextOverflow.ellipsis)),
        DropdownMenuItem(value: 'refunded', child: Text('Refunded', overflow: TextOverflow.ellipsis)),
      ],
      onChanged: (value) => _apply(() => _paymentFilter = value),
    );
  }
}

Widget _paymentChip(PaymentStatus? status) {
  switch (status) {
    case PaymentStatus.completed:
      return labeledStatusChip(label: 'Completed', tone: 'success');
    case PaymentStatus.pending:
      return labeledStatusChip(label: 'Pending', tone: 'warning');
    case PaymentStatus.failed:
      return labeledStatusChip(label: 'Failed', tone: 'danger');
    case PaymentStatus.refunded:
      return labeledStatusChip(label: 'Refunded', tone: 'neutral');
    case null:
      return labeledStatusChip(label: 'No payment', tone: 'neutral');
  }
}

String _text(String? value) {
  final text = value?.trim() ?? '';
  return text.isEmpty ? '—' : text;
}

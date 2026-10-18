import 'dart:typed_data';

import 'package:ecommerce_desktop/models/activity.dart';
import 'package:ecommerce_desktop/models/reservation.dart';
import 'package:ecommerce_desktop/models/zoo_partner.dart';
import 'package:ecommerce_desktop/providers/activity_provider.dart';
import 'package:ecommerce_desktop/providers/report_provider.dart';
import 'package:ecommerce_desktop/providers/zoo_partner_provider.dart';
import 'package:ecommerce_desktop/theme/app_colors.dart';
import 'package:ecommerce_desktop/utils/api_client_exception.dart';
import 'package:ecommerce_desktop/utils/formatters.dart';
import 'package:ecommerce_desktop/utils/pdf_launcher.dart';
import 'package:ecommerce_desktop/widgets/admin_list.dart';
import 'package:file_selector/file_selector.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

enum ReportKind { reservations, donations }

class ReportsScreen extends StatefulWidget {
  const ReportsScreen({super.key});

  @override
  State<ReportsScreen> createState() => _ReportsScreenState();
}

class _ReportsScreenState extends State<ReportsScreen> {
  ReportKind _kind = ReportKind.reservations;
  DateTime? _dateFrom;
  DateTime? _dateTo;
  int? _activityId;
  ReservationStatus? _status;
  int? _partnerId;
  bool _loadingActivities = true;
  bool _loadingPartners = true;
  bool _busy = false;
  String? _resultError;
  Uint8List? _bytes;
  String? _savedPath;
  List<Activity> _activities = [];
  List<ZooPartner> _partners = [];

  @override
  void initState() {
    super.initState();
    final today = DateTime.now();
    _dateFrom = DateTime(today.year, today.month, 1);
    _dateTo = DateTime(today.year, today.month, today.day);
    _loadActivities();
    _loadPartners();
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

  String? get _fromError {
    if (_dateFrom == null) return 'From date is required.';
    return null;
  }

  String? get _toError {
    if (_dateTo == null) return 'To date is required.';
    if (_dateFrom != null && DateUtils.dateOnly(_dateFrom!).isAfter(DateUtils.dateOnly(_dateTo!))) {
      return 'From date cannot be after to date.';
    }
    return null;
  }

  String? get _disabledReason {
    if (_busy) return 'The report is being generated.';
    return _fromError ?? _toError;
  }

  void _edit(VoidCallback change) {
    setState(() {
      change();
      _bytes = null;
      _resultError = null;
      _savedPath = null;
    });
  }

  Future<void> _pickDate({required bool from}) async {
    final current = from ? _dateFrom : _dateTo;
    final today = DateTime.now();
    final picked = await showDatePicker(
      context: context,
      initialDate: current ?? DateTime(today.year, today.month, today.day),
      firstDate: DateTime(2000),
      lastDate: DateTime(2100, 12, 31),
    );
    if (picked == null || !mounted) return;
    final day = DateTime(picked.year, picked.month, picked.day);
    _edit(() {
      if (from) {
        _dateFrom = day;
      } else {
        _dateTo = day;
      }
    });
  }

  String _fileName() {
    final from = formatApiDate(_dateFrom!);
    final to = formatApiDate(_dateTo!);
    final prefix = _kind == ReportKind.reservations ? 'reservations' : 'donations-by-partner';
    return '$prefix-$from-to-$to.pdf';
  }

  Future<void> _generate() async {
    if (_disabledReason != null || _dateFrom == null || _dateTo == null) return;
    setState(() {
      _busy = true;
      _resultError = null;
      _bytes = null;
      _savedPath = null;
    });
    try {
      final Uint8List bytes;
      if (_kind == ReportKind.reservations) {
        bytes = await context.read<ReportProvider>().reservationsPdf(
          dateFrom: _dateFrom!,
          dateTo: _dateTo!,
          activityId: _activityId,
          status: _status == null ? null : reservationStatusApiValue(_status!),
        );
      } else {
        bytes = await context.read<ReportProvider>().donationsPdf(
          dateFrom: _dateFrom!,
          dateTo: _dateTo!,
          zooPartnerId: _partnerId,
        );
      }
      if (!mounted) return;
      setState(() {
        _bytes = bytes;
        _busy = false;
      });
    } on SessionExpiredException {
      return;
    } catch (error) {
      if (!mounted) return;
      setState(() {
        _resultError = friendlyError(error);
        _busy = false;
      });
    }
  }

  Future<void> _save() async {
    final bytes = _bytes;
    if (bytes == null) return;
    final location = await getSaveLocation(
      suggestedName: _fileName(),
      acceptedTypeGroups: const [
        XTypeGroup(label: 'PDF', extensions: ['pdf']),
      ],
    );
    if (location == null || !mounted) return;
    var path = location.path;
    if (!path.toLowerCase().endsWith('.pdf')) {
      path = '$path.pdf';
    }
    try {
      await XFile.fromData(bytes, mimeType: 'application/pdf', name: _fileName()).saveTo(path);
      if (!mounted) return;
      setState(() => _savedPath = path);
      _toast('Report saved.');
    } catch (_) {
      if (!mounted) return;
      _toast('The PDF could not be saved.');
    }
  }

  Future<void> _open() async {
    final bytes = _bytes;
    if (bytes == null) return;
    final message = await openPdfBytes(
      fileName: _fileName(),
      bytes: bytes,
      savedPath: _savedPath,
    );
    if (!mounted || message == null) return;
    _toast(message);
  }

  void _toast(String message) {
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(message)));
  }

  @override
  Widget build(BuildContext context) {
    return AdminScreenFrame(
      title: 'Reports',
      subtitle: 'Generate a PDF, then save it or open it to print.',
      filters: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Wrap(
            spacing: 8,
            runSpacing: 8,
            children: [
              ChoiceChip(
                label: const Text('Reservations by period'),
                selected: _kind == ReportKind.reservations,
                onSelected: (selected) {
                  if (selected) _edit(() => _kind = ReportKind.reservations);
                },
              ),
              ChoiceChip(
                label: const Text('Donations by zoo partner'),
                selected: _kind == ReportKind.donations,
                onSelected: (selected) {
                  if (selected) _edit(() => _kind = ReportKind.donations);
                },
              ),
            ],
          ),
          const SizedBox(height: 12),
          Wrap(
            spacing: 12,
            runSpacing: 12,
            crossAxisAlignment: WrapCrossAlignment.start,
            children: [
              _dateField(label: 'From date', value: _dateFrom, from: true, error: _fromError),
              _dateField(label: 'To date', value: _dateTo, from: false, error: _toError),
              if (_kind == ReportKind.reservations) ...[
                SizedBox(width: 240, child: _activityField()),
                SizedBox(width: 180, child: _statusField()),
              ] else
                SizedBox(width: 240, child: _partnerField()),
              _generateButton(),
            ],
          ),
        ],
      ),
      body: _result(),
    );
  }

  Widget _dateField({
    required String label,
    required DateTime? value,
    required bool from,
    required String? error,
  }) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        DateFilterField(
          label: label,
          value: value,
          onPick: () => _pickDate(from: from),
          onClear: value == null
              ? null
              : () => _edit(() {
                  if (from) {
                    _dateFrom = null;
                  } else {
                    _dateTo = null;
                  }
                }),
        ),
        if (error != null)
          const SizedBox(height: 4),
        if (error != null)
          SizedBox(
            width: 210,
            child: Text(error, style: const TextStyle(color: AppColors.danger, fontSize: 12)),
          ),
      ],
    );
  }

  Widget _activityField() {
    if (_loadingActivities) {
      return const InputDecorator(
        decoration: InputDecoration(labelText: 'Activity', isDense: true),
        child: Text('Loading activities.', overflow: TextOverflow.ellipsis),
      );
    }
    final selected = _activities.any((activity) => activity.id == _activityId) ? _activityId : null;
    return DropdownButtonFormField<int?>(
      key: ValueKey('report-activity-$selected-${_activities.length}'),
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
      onChanged: (value) => _edit(() => _activityId = value),
    );
  }

  Widget _statusField() {
    return DropdownButtonFormField<ReservationStatus?>(
      key: ValueKey('report-status-$_status'),
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
      onChanged: (value) => _edit(() => _status = value),
    );
  }

  Widget _partnerField() {
    if (_loadingPartners) {
      return const InputDecorator(
        decoration: InputDecoration(labelText: 'Zoo partner', isDense: true),
        child: Text('Loading zoo partners.', overflow: TextOverflow.ellipsis),
      );
    }
    final selected = _partners.any((partner) => partner.id == _partnerId) ? _partnerId : null;
    return DropdownButtonFormField<int?>(
      key: ValueKey('report-partner-$selected-${_partners.length}'),
      initialValue: selected,
      isExpanded: true,
      decoration: const InputDecoration(labelText: 'Zoo partner', isDense: true),
      items: [
        const DropdownMenuItem(value: null, child: Text('All partners', overflow: TextOverflow.ellipsis)),
        for (final partner in _partners)
          DropdownMenuItem(
            value: partner.id,
            child: Text(
              partner.isActive ? partner.name : '${partner.name} (inactive)',
              overflow: TextOverflow.ellipsis,
            ),
          ),
      ],
      onChanged: (value) => _edit(() => _partnerId = value),
    );
  }

  Widget _generateButton() {
    final reason = _disabledReason;
    return Padding(
      padding: const EdgeInsets.only(top: 2),
      child: Tooltip(
        message: reason ?? 'Generate the PDF for the selected filters.',
        child: FilledButton.icon(
          onPressed: reason == null ? _generate : null,
          icon: const Icon(Icons.picture_as_pdf_outlined),
          label: const Text('Generate PDF'),
        ),
      ),
    );
  }

  Widget _result() {
    if (_busy) {
      return const _MessageCard(
        title: 'Generating the report.',
        message: 'This can take a moment while the PDF is created.',
        progress: true,
      );
    }
    if (_resultError != null) {
      return _MessageCard(title: 'Report not created.', message: _resultError!);
    }
    final bytes = _bytes;
    if (bytes == null) {
      return const _MessageCard(
        title: 'No report yet.',
        message: 'Choose a period and generate a PDF. An empty result shows a message instead of a blank file.',
      );
    }
    return _MessageCard(
      title: 'Report ready.',
      message: '${_fileName()} · ${_sizeLabel(bytes.length)}',
      actions: [
        FilledButton.icon(
          onPressed: _save,
          icon: const Icon(Icons.save_alt),
          label: const Text('Save PDF'),
        ),
        OutlinedButton.icon(
          onPressed: _open,
          icon: const Icon(Icons.open_in_new),
          label: const Text('Open PDF'),
        ),
      ],
    );
  }

  String _sizeLabel(int bytes) {
    if (bytes < 1024) return '$bytes B';
    final kilobytes = bytes / 1024;
    if (kilobytes == kilobytes.roundToDouble()) return '${kilobytes.toInt()} KB';
    return '${kilobytes.toStringAsFixed(1)} KB';
  }
}

class _MessageCard extends StatelessWidget {
  const _MessageCard({
    required this.title,
    required this.message,
    this.progress = false,
    this.actions = const [],
  });

  final String title;
  final String message;
  final bool progress;
  final List<Widget> actions;

  @override
  Widget build(BuildContext context) {
    return Align(
      alignment: Alignment.topLeft,
      child: Container(
        width: double.infinity,
        padding: const EdgeInsets.all(18),
        decoration: BoxDecoration(
          color: AppColors.surface,
          borderRadius: BorderRadius.circular(14),
          border: Border.all(color: AppColors.border),
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          mainAxisSize: MainAxisSize.min,
          children: [
            Text(title, style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w700)),
            const SizedBox(height: 6),
            Text(message, style: const TextStyle(color: AppColors.textMuted)),
            if (progress) ...[
              const SizedBox(height: 16),
              const LinearProgressIndicator(),
            ],
            if (actions.isNotEmpty) ...[
              const SizedBox(height: 16),
              Wrap(spacing: 12, runSpacing: 12, children: actions),
            ],
          ],
        ),
      ),
    );
  }
}

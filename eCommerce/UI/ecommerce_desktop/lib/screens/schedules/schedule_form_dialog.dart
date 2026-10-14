import 'package:ecommerce_desktop/models/activity.dart';
import 'package:ecommerce_desktop/models/schedule.dart';
import 'package:ecommerce_desktop/providers/activity_provider.dart';
import 'package:ecommerce_desktop/providers/schedule_provider.dart';
import 'package:ecommerce_desktop/theme/app_colors.dart';
import 'package:ecommerce_desktop/utils/api_client_exception.dart';
import 'package:ecommerce_desktop/utils/formatters.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:provider/provider.dart';

Future<bool> showScheduleFormDialog(BuildContext context, {Schedule? schedule}) async {
  final saved = await showDialog<bool>(
    context: context,
    builder: (_) => ScheduleFormDialog(schedule: schedule),
  );
  return saved == true;
}

class ScheduleFormDialog extends StatefulWidget {
  const ScheduleFormDialog({super.key, this.schedule});

  final Schedule? schedule;

  @override
  State<ScheduleFormDialog> createState() => _ScheduleFormDialogState();
}

class _ScheduleFormDialogState extends State<ScheduleFormDialog> {
  final _formKey = GlobalKey<FormState>();
  late final TextEditingController _capacity;

  List<Activity> _activities = [];
  int? _activityId;
  late DateTime _date;
  late TimeOfDay _start;
  late TimeOfDay _end;
  bool _loadingActivities = true;
  bool _saving = false;
  String? _error;

  bool get _isEdit => widget.schedule != null;

  @override
  void initState() {
    super.initState();
    final schedule = widget.schedule;
    final now = DateTime.now();
    _date = schedule?.date ?? DateTime(now.year, now.month, now.day);
    _start = _timeFromDuration(schedule?.startTime) ?? const TimeOfDay(hour: 9, minute: 0);
    _end = _timeFromDuration(schedule?.endTime) ?? const TimeOfDay(hour: 10, minute: 0);
    _activityId = schedule?.activityId;
    _capacity = TextEditingController(
      text: schedule == null ? '' : schedule.capacityLeft.toString(),
    );
    _loadActivities();
  }

  TimeOfDay? _timeFromDuration(Duration? duration) {
    if (duration == null) return null;
    return TimeOfDay(hour: duration.inHours, minute: duration.inMinutes.remainder(60));
  }

  @override
  void dispose() {
    _capacity.dispose();
    super.dispose();
  }

  Future<void> _loadActivities() async {
    try {
      final provider = context.read<ActivityProvider>();
      final result = await provider.get(
        filter: const {
          'Status': 'Active',
          'Page': 1,
          'PageSize': 100,
        },
      );
      if (!mounted) return;
      final items = List<Activity>.from(result.items ?? []);
      final currentId = widget.schedule?.activityId;
      if (currentId != null && items.every((activity) => activity.id != currentId)) {
        items.insert(
          0,
          Activity(
            id: currentId,
            name: widget.schedule?.activityName ?? 'Aktivnost $currentId',
            capacity: widget.schedule?.activityCapacity ?? 0,
            status: ActivityStatus.inactive,
          ),
        );
      }
      setState(() {
        _activities = items;
        _loadingActivities = false;
      });
    } on SessionExpiredException {
      return;
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _loadingActivities = false;
        _error = e.toString().replaceFirst('Bad state: ', '');
      });
    }
  }

  Activity? get _selectedActivity {
    for (final activity in _activities) {
      if (activity.id == _activityId) return activity;
    }
    return null;
  }

  int _minutes(TimeOfDay time) => time.hour * 60 + time.minute;

  Future<void> _pickDate() async {
    final today = DateUtils.dateOnly(DateTime.now());
    final current = DateUtils.dateOnly(_date);
    final firstDate = current.isBefore(today) ? current : today;
    final picked = await showDatePicker(
      context: context,
      initialDate: current.isBefore(firstDate) ? firstDate : current,
      firstDate: firstDate,
      lastDate: DateTime(today.year + 3, 12, 31),
    );
    if (picked == null || !mounted) return;
    setState(() => _date = DateTime(picked.year, picked.month, picked.day));
  }

  Future<void> _pickTime({required bool start}) async {
    final picked = await showTimePicker(
      context: context,
      initialTime: start ? _start : _end,
    );
    if (picked == null || !mounted) return;
    setState(() {
      if (start) {
        _start = picked;
      } else {
        _end = picked;
      }
    });
  }

  String _clock(TimeOfDay time) {
    final hour = time.hour.toString().padLeft(2, '0');
    final minute = time.minute.toString().padLeft(2, '0');
    return '$hour:$minute';
  }

  String _apiTime(TimeOfDay time) {
    final hour = time.hour.toString().padLeft(2, '0');
    final minute = time.minute.toString().padLeft(2, '0');
    return '$hour:$minute:00';
  }

  Future<void> _save() async {
    if (!_formKey.currentState!.validate()) return;
    if (_activityId == null) {
      setState(() => _error = 'Odaberite aktivnost.');
      return;
    }

    final body = {
      'activityId': _activityId,
      'date': formatApiDate(_date),
      'startTime': _apiTime(_start),
      'endTime': _apiTime(_end),
      'capacityLeft': int.parse(_capacity.text.trim()),
      'status': _isEdit
          ? scheduleStatusApiValue(widget.schedule!.status)
          : scheduleStatusApiValue(ScheduleStatus.active),
    };

    setState(() {
      _saving = true;
      _error = null;
    });

    try {
      final provider = context.read<ScheduleProvider>();
      if (_isEdit) {
        await provider.update(widget.schedule!.id, body);
      } else {
        await provider.insert(body);
      }
      if (mounted) Navigator.pop(context, true);
    } on SessionExpiredException {
      return;
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _saving = false;
        _error = e.toString().replaceFirst('Bad state: ', '');
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    return Dialog(
      child: ConstrainedBox(
        constraints: const BoxConstraints(maxWidth: 640, maxHeight: 680),
        child: Padding(
          padding: const EdgeInsets.fromLTRB(24, 22, 24, 18),
          child: Form(
            key: _formKey,
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Text(
                  _isEdit ? 'Uredi termin' : 'Novi termin',
                  style: const TextStyle(fontSize: 20, fontWeight: FontWeight.w700),
                ),
                const SizedBox(height: 16),
                Flexible(
                  child: SingleChildScrollView(
                    child: Column(
                      children: [
                        _activityField(),
                        const SizedBox(height: 12),
                        _dateField(),
                        const SizedBox(height: 12),
                        Row(
                          children: [
                            Expanded(child: _timeField(start: true)),
                            const SizedBox(width: 12),
                            Expanded(child: _timeField(start: false)),
                          ],
                        ),
                        const SizedBox(height: 12),
                        _capacityField(),
                        if (_error != null) ...[
                          const SizedBox(height: 8),
                          Text(_error!, style: const TextStyle(color: AppColors.danger)),
                        ],
                      ],
                    ),
                  ),
                ),
                const SizedBox(height: 12),
                Row(
                  mainAxisAlignment: MainAxisAlignment.end,
                  children: [
                    TextButton(
                      onPressed: _saving ? null : () => Navigator.pop(context, false),
                      child: const Text('Odustani'),
                    ),
                    const SizedBox(width: 8),
                    FilledButton(
                      onPressed: _saving || _loadingActivities ? null : _save,
                      child: Text(_saving ? 'Spremanje...' : 'Sačuvaj'),
                    ),
                  ],
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }

  Widget _activityField() {
    if (_loadingActivities) {
      return const InputDecorator(
        decoration: InputDecoration(labelText: 'Aktivnost'),
        child: Text('Učitavanje...'),
      );
    }

    if (_activities.isEmpty) {
      return const InputDecorator(
        decoration: InputDecoration(labelText: 'Aktivnost'),
        child: Text('Nema aktivnih aktivnosti.'),
      );
    }

    final selected = _activities.any((activity) => activity.id == _activityId)
        ? _activityId
        : null;
    return DropdownButtonFormField<int>(
      initialValue: selected,
      isExpanded: true,
      decoration: const InputDecoration(labelText: 'Aktivnost'),
      items: [
        for (final activity in _activities)
          DropdownMenuItem(value: activity.id, child: Text(activity.name)),
      ],
      onChanged: (value) {
        final activity = _activities.where((item) => item.id == value).firstOrNull;
        setState(() {
          _activityId = value;
          if (activity != null) {
            _capacity.text = activity.capacity.toString();
          }
        });
      },
      validator: (value) => value == null ? 'Aktivnost je obavezna.' : null,
    );
  }

  Widget _dateField() {
    final today = DateUtils.dateOnly(DateTime.now());
    return FormField<DateTime>(
      initialValue: _date,
      validator: (value) {
        final date = value ?? _date;
        final day = DateUtils.dateOnly(date);
        final original = widget.schedule == null
            ? null
            : DateUtils.dateOnly(widget.schedule!.date);
        if (day.isBefore(today) && day != original) {
          return 'Datum ne može biti u prošlosti.';
        }
        return null;
      },
      builder: (state) {
        return InkWell(
          onTap: () async {
            await _pickDate();
            state.didChange(_date);
          },
          borderRadius: BorderRadius.circular(10),
          child: InputDecorator(
            decoration: InputDecoration(
              labelText: 'Datum',
              errorText: state.errorText,
              suffixIcon: const Icon(Icons.calendar_today_outlined, size: 18),
            ),
            child: Text(formatDate(_date)),
          ),
        );
      },
    );
  }

  Widget _timeField({required bool start}) {
    final value = start ? _start : _end;
    return FormField<TimeOfDay>(
      initialValue: value,
      validator: (_) {
        if (_minutes(_end) <= _minutes(_start)) {
          return start ? null : 'Završetak mora biti nakon početka.';
        }
        return null;
      },
      builder: (state) {
        return InkWell(
          onTap: () async {
            await _pickTime(start: start);
            state.didChange(start ? _start : _end);
          },
          borderRadius: BorderRadius.circular(10),
          child: InputDecorator(
            decoration: InputDecoration(
              labelText: start ? 'Početak' : 'Završetak',
              errorText: state.errorText,
              suffixIcon: const Icon(Icons.schedule, size: 18),
            ),
            child: Text(_clock(value)),
          ),
        );
      },
    );
  }

  Widget _capacityField() {
    return TextFormField(
      controller: _capacity,
      decoration: const InputDecoration(labelText: 'Preostali kapacitet'),
      keyboardType: TextInputType.number,
      inputFormatters: [FilteringTextInputFormatter.digitsOnly],
      validator: (value) {
        final parsed = int.tryParse(value?.trim() ?? '');
        if (parsed == null) return 'Unesite preostali kapacitet.';
        if (parsed < 0) return 'Preostali kapacitet ne može biti negativan.';
        final maxCapacity = _selectedActivity?.capacity;
        if (maxCapacity != null && maxCapacity > 0 && parsed > maxCapacity) {
          return 'Preostali kapacitet ne može biti veći od kapaciteta aktivnosti ($maxCapacity).';
        }
        return null;
      },
    );
  }
}

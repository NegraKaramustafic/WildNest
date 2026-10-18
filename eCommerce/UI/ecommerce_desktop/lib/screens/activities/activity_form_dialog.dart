import 'package:ecommerce_desktop/models/activity.dart';
import 'package:ecommerce_desktop/models/activity_type.dart';
import 'package:ecommerce_desktop/models/animal.dart';
import 'package:ecommerce_desktop/providers/activity_provider.dart';
import 'package:ecommerce_desktop/providers/animal_provider.dart';
import 'package:ecommerce_desktop/theme/app_colors.dart';
import 'package:ecommerce_desktop/utils/api_client_exception.dart';
import 'package:ecommerce_desktop/utils/formatters.dart';
import 'package:ecommerce_desktop/utils/json_parsers.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:provider/provider.dart';

class ActivityFormResult {
  const ActivityFormResult({this.created});

  final Activity? created;
}

Future<ActivityFormResult?> showActivityFormDialog(
  BuildContext context, {
  Activity? activity,
  required List<ActivityType> types,
}) {
  if (types.isEmpty) {
    return showDialog<ActivityFormResult>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('No activity types'),
        content: const Text(
          'Add an activity type before adding an activity.',
        ),
        actions: [
          FilledButton(
            onPressed: () => Navigator.pop(context),
            child: const Text('OK'),
          ),
        ],
      ),
    );
  }

  return showDialog<ActivityFormResult>(
    context: context,
    builder: (_) => ActivityFormDialog(activity: activity, types: types),
  );
}

class ActivityFormDialog extends StatefulWidget {
  const ActivityFormDialog({super.key, this.activity, required this.types});

  final Activity? activity;
  final List<ActivityType> types;

  @override
  State<ActivityFormDialog> createState() => _ActivityFormDialogState();
}

class _ActivityFormDialogState extends State<ActivityFormDialog> {
  final _formKey = GlobalKey<FormState>();
  late final TextEditingController _name;
  late final TextEditingController _description;
  late final TextEditingController _duration;
  late final TextEditingController _price;
  late final TextEditingController _capacity;
  late final TextEditingController _imageUrl;

  List<Animal> _animals = [];
  late List<ActivityType> _types;
  int? _animalId;
  int? _activityTypeId;
  bool _loadingAnimals = true;
  bool _saving = false;
  String? _error;

  bool get _isEdit => widget.activity != null;

  @override
  void initState() {
    super.initState();
    final activity = widget.activity;
    _name = TextEditingController(text: activity?.name ?? '');
    _description = TextEditingController(text: activity?.description ?? '');
    _duration = TextEditingController(
      text: activity == null ? '' : activity.duration.inMinutes.toString(),
    );
    _price = TextEditingController(
      text: activity == null ? '' : _numberText(activity.price),
    );
    _capacity = TextEditingController(
      text: activity == null ? '' : activity.capacity.toString(),
    );
    _imageUrl = TextEditingController(text: activity?.imageUrl ?? '');
    _animalId = activity?.animalId;
    _types = List<ActivityType>.of(widget.types);
    _activityTypeId = activity?.activityTypeId;
    if (_activityTypeId != null && _types.every((type) => type.id != _activityTypeId)) {
      _types = [
        ActivityType(
          id: _activityTypeId!,
          name: activity?.activityTypeName.isNotEmpty == true
              ? activity!.activityTypeName
              : 'Activity type',
        ),
        ..._types,
      ];
    }
    _activityTypeId ??= _types.first.id;
    _loadAnimals();
  }

  String _numberText(double value) {
    if (value == value.roundToDouble()) return value.toInt().toString();
    return value.toString();
  }

  @override
  void dispose() {
    _name.dispose();
    _description.dispose();
    _duration.dispose();
    _price.dispose();
    _capacity.dispose();
    _imageUrl.dispose();
    super.dispose();
  }

  Future<void> _loadAnimals() async {
    try {
      final provider = context.read<AnimalProvider>();
      final result = await provider.get(
        filter: const {'Page': 1, 'PageSize': 100},
      );
      if (!mounted) return;
      final items = result.items ?? [];
      setState(() {
        _animals = items;
        _loadingAnimals = false;
        if (_animalId != null && items.every((animal) => animal.id != _animalId)) {
          _animals = [
            Animal(
              id: _animalId!,
              name: widget.activity?.animalName ?? 'Animal $_animalId',
              species: '',
              categoryId: 0,
              categoryName: '',
            ),
            ...items,
          ];
        }
      });
    } on SessionExpiredException {
      return;
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _loadingAnimals = false;
        _error = e.toString().replaceFirst('Bad state: ', '');
      });
    }
  }

  double? _parseNumber(String raw) {
    final text = raw.trim().replaceAll(',', '.');
    if (text.isEmpty) return null;
    return double.tryParse(text);
  }

  Future<void> _save() async {
    if (!_formKey.currentState!.validate()) return;

    final minutes = int.parse(_duration.text.trim());
    final body = {
      'name': _name.text.trim(),
      'description': blankToNull(_description.text),
      'animalId': _animalId,
      'activityTypeId': _activityTypeId,
      'duration': minutesToTimeSpan(minutes),
      'price': _parseNumber(_price.text),
      'capacity': int.parse(_capacity.text.trim()),
      'imageUrl': blankToNull(_imageUrl.text),
    };

    setState(() {
      _saving = true;
      _error = null;
    });

    try {
      final provider = context.read<ActivityProvider>();
      if (_isEdit) {
        await provider.update(widget.activity!.id, body);
        if (mounted) Navigator.pop(context, const ActivityFormResult());
      } else {
        final created = await provider.insert(body);
        if (mounted) Navigator.pop(context, ActivityFormResult(created: created));
      }
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
        constraints: const BoxConstraints(maxWidth: 720, maxHeight: 760),
        child: Padding(
          padding: const EdgeInsets.fromLTRB(24, 22, 24, 18),
          child: Form(
            key: _formKey,
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Text(
                  _isEdit ? 'Edit activity' : 'New activity',
                  style: const TextStyle(fontSize: 20, fontWeight: FontWeight.w700),
                ),
                if (!_isEdit) ...[
                  const SizedBox(height: 6),
                  const Text(
                    'A new activity is saved as a draft.',
                    style: TextStyle(color: AppColors.textMuted),
                  ),
                ],
                const SizedBox(height: 16),
                Flexible(
                  child: SingleChildScrollView(
                    child: Column(
                      children: [
                        Row(
                          children: [
                            Expanded(child: _nameField()),
                            const SizedBox(width: 12),
                            Expanded(child: _typeField()),
                          ],
                        ),
                        const SizedBox(height: 12),
                        _animalField(),
                        const SizedBox(height: 12),
                        Row(
                          children: [
                            Expanded(child: _durationField()),
                            const SizedBox(width: 12),
                            Expanded(child: _priceField()),
                            const SizedBox(width: 12),
                            Expanded(child: _capacityField()),
                          ],
                        ),
                        const SizedBox(height: 12),
                        _imageField(),
                        const SizedBox(height: 12),
                        TextFormField(
                          controller: _description,
                          maxLines: 3,
                          maxLength: 1000,
                          decoration: const InputDecoration(
                            labelText: 'Description',
                            alignLabelWithHint: true,
                          ),
                          validator: (value) {
                            if ((value?.trim().length ?? 0) > 1000) {
                              return 'Description cannot be longer than 1000 characters.';
                            }
                            return null;
                          },
                        ),
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
                      onPressed: _saving ? null : () => Navigator.pop(context),
                      child: const Text('Cancel'),
                    ),
                    const SizedBox(width: 8),
                    FilledButton(
                      onPressed: _saving || _loadingAnimals ? null : _save,
                      child: Text(_saving ? 'Saving...' : 'Save'),
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

  Widget _nameField() {
    return TextFormField(
      controller: _name,
      decoration: const InputDecoration(labelText: 'Name'),
      validator: (value) {
        final text = value?.trim() ?? '';
        if (text.isEmpty) return 'Name is required.';
        if (text.length > 100) return 'Name cannot be longer than 100 characters.';
        return null;
      },
    );
  }

  Widget _typeField() {
    final selected = _types.any((type) => type.id == _activityTypeId) ? _activityTypeId : null;
    return DropdownButtonFormField<int>(
      key: ValueKey('activity-type-$selected'),
      initialValue: selected,
      isExpanded: true,
      decoration: const InputDecoration(labelText: 'Type'),
      items: [
        for (final type in _types)
          DropdownMenuItem(value: type.id, child: Text(type.name)),
      ],
      onChanged: (value) {
        if (value != null) setState(() => _activityTypeId = value);
      },
      validator: (value) => value == null ? 'Type is required.' : null,
    );
  }

  Widget _animalField() {
    if (_loadingAnimals) {
      return const InputDecorator(
        decoration: InputDecoration(labelText: 'Animal'),
        child: Text('Loading...'),
      );
    }

    final selected = _animals.any((animal) => animal.id == _animalId) ? _animalId : null;
    return DropdownButtonFormField<int?>(
      initialValue: selected,
      decoration: const InputDecoration(labelText: 'Animal'),
      items: [
        const DropdownMenuItem<int?>(value: null, child: Text('No animal')),
        for (final animal in _animals)
          DropdownMenuItem(value: animal.id, child: Text(animal.name)),
      ],
      onChanged: (value) => setState(() => _animalId = value),
    );
  }

  Widget _durationField() {
    return TextFormField(
      controller: _duration,
      decoration: const InputDecoration(labelText: 'Duration (minutes)'),
      keyboardType: TextInputType.number,
      inputFormatters: [FilteringTextInputFormatter.digitsOnly],
      validator: (value) {
        final parsed = int.tryParse(value?.trim() ?? '');
        if (parsed == null || parsed <= 0) {
          return 'Duration must be greater than 0.';
        }
        return null;
      },
    );
  }

  Widget _priceField() {
    return TextFormField(
      controller: _price,
      decoration: const InputDecoration(labelText: 'Price (€)'),
      keyboardType: const TextInputType.numberWithOptions(decimal: true),
      inputFormatters: [FilteringTextInputFormatter.allow(RegExp(r'[0-9.,]'))],
      validator: (value) {
        final parsed = _parseNumber(value ?? '');
        if (parsed == null) return 'Enter a valid price.';
        if (parsed <= 0) return 'Price must be greater than 0.';
        return null;
      },
    );
  }

  Widget _capacityField() {
    return TextFormField(
      controller: _capacity,
      decoration: const InputDecoration(labelText: 'Capacity'),
      keyboardType: TextInputType.number,
      inputFormatters: [FilteringTextInputFormatter.digitsOnly],
      validator: (value) {
        final parsed = int.tryParse(value?.trim() ?? '');
        if (parsed == null || parsed <= 0) {
          return 'Capacity must be greater than 0.';
        }
        return null;
      },
    );
  }

  Widget _imageField() {
    return TextFormField(
      controller: _imageUrl,
      decoration: const InputDecoration(labelText: 'Image URL'),
      validator: (value) {
        if ((value?.trim().length ?? 0) > 500) {
          return 'Image URL cannot be longer than 500 characters.';
        }
        return null;
      },
    );
  }
}

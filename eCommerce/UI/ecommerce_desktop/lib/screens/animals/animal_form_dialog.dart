import 'package:ecommerce_desktop/models/animal.dart';
import 'package:ecommerce_desktop/models/animal_category.dart';
import 'package:ecommerce_desktop/providers/animal_category_provider.dart';
import 'package:ecommerce_desktop/providers/animal_provider.dart';
import 'package:ecommerce_desktop/theme/app_colors.dart';
import 'package:ecommerce_desktop/utils/api_client_exception.dart';
import 'package:ecommerce_desktop/utils/json_parsers.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:provider/provider.dart';

Future<bool> showAnimalFormDialog(BuildContext context, {Animal? animal}) async {
  final saved = await showDialog<bool>(
    context: context,
    builder: (_) => AnimalFormDialog(animal: animal),
  );
  return saved == true;
}

class AnimalFormDialog extends StatefulWidget {
  const AnimalFormDialog({super.key, this.animal});

  final Animal? animal;

  @override
  State<AnimalFormDialog> createState() => _AnimalFormDialogState();
}

class _AnimalFormDialogState extends State<AnimalFormDialog> {
  final _formKey = GlobalKey<FormState>();
  late final TextEditingController _name;
  late final TextEditingController _species;
  late final TextEditingController _habitat;
  late final TextEditingController _diet;
  late final TextEditingController _weight;
  late final TextEditingController _lifespan;
  late final TextEditingController _description;
  late final TextEditingController _imageUrl;
  late final TextEditingController _adoptionPrice;

  List<AnimalCategory> _categories = [];
  int? _categoryId;
  AnimalTag _tag = AnimalTag.none;
  bool _isActive = true;
  bool _loadingCategories = true;
  bool _saving = false;
  String? _error;

  bool get _isEdit => widget.animal != null;

  @override
  void initState() {
    super.initState();
    final animal = widget.animal;
    _name = TextEditingController(text: animal?.name ?? '');
    _species = TextEditingController(text: animal?.species ?? '');
    _habitat = TextEditingController(text: animal?.habitat ?? '');
    _diet = TextEditingController(text: animal?.diet ?? '');
    _weight = TextEditingController(
      text: animal == null ? '' : _numberText(animal.weight),
    );
    _lifespan = TextEditingController(text: animal?.lifespan ?? '');
    _description = TextEditingController(text: animal?.description ?? '');
    _imageUrl = TextEditingController(text: animal?.imageUrl ?? '');
    _adoptionPrice = TextEditingController(
      text: animal?.adoptionPrice == null ? '' : _numberText(animal!.adoptionPrice!),
    );
    _categoryId = animal?.categoryId;
    _tag = animal?.tag ?? AnimalTag.none;
    _isActive = animal?.isActive ?? true;
    _loadCategories();
  }

  String _numberText(double value) {
    if (value == value.roundToDouble()) return value.toInt().toString();
    return value.toString();
  }

  @override
  void dispose() {
    _name.dispose();
    _species.dispose();
    _habitat.dispose();
    _diet.dispose();
    _weight.dispose();
    _lifespan.dispose();
    _description.dispose();
    _imageUrl.dispose();
    _adoptionPrice.dispose();
    super.dispose();
  }

  Future<void> _loadCategories() async {
    try {
      final result = await context.read<AnimalCategoryProvider>().get(
        filter: const {'Page': 1, 'PageSize': 100},
      );
      if (!mounted) return;
      final items = result.items ?? [];
      setState(() {
        _categories = items;
        _loadingCategories = false;
        if (_categoryId != null && items.every((c) => c.id != _categoryId)) {
          _categories = [
            AnimalCategory(
              id: _categoryId!,
              name: widget.animal?.categoryName ?? 'Kategorija $_categoryId',
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
        _loadingCategories = false;
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
    if (_categoryId == null) {
      setState(() => _error = 'Odaberite kategoriju.');
      return;
    }

    final body = {
      'name': _name.text.trim(),
      'species': _species.text.trim(),
      'categoryId': _categoryId,
      'habitat': blankToNull(_habitat.text),
      'diet': blankToNull(_diet.text),
      'weight': _parseNumber(_weight.text) ?? 0,
      'lifespan': blankToNull(_lifespan.text),
      'description': blankToNull(_description.text),
      'imageUrl': blankToNull(_imageUrl.text),
      'tag': animalTagApiValue(_tag),
      'isActive': _isActive,
      'adoptionPrice': _parseNumber(_adoptionPrice.text),
    };

    setState(() {
      _saving = true;
      _error = null;
    });

    try {
      final provider = context.read<AnimalProvider>();
      if (_isEdit) {
        await provider.update(widget.animal!.id, body);
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
                  _isEdit ? 'Edit animal' : 'New animal',
                  style: const TextStyle(fontSize: 20, fontWeight: FontWeight.w700),
                ),
                const SizedBox(height: 16),
                Flexible(
                  child: SingleChildScrollView(
                    child: Column(
                      children: [
                        Row(
                          children: [
                            Expanded(child: _nameField()),
                            const SizedBox(width: 12),
                            Expanded(child: _speciesField()),
                          ],
                        ),
                        const SizedBox(height: 12),
                        Row(
                          children: [
                            Expanded(child: _categoryField()),
                            const SizedBox(width: 12),
                            Expanded(child: _tagField()),
                          ],
                        ),
                        const SizedBox(height: 12),
                        Row(
                          children: [
                            Expanded(child: _optionalField(_habitat, 'Habitat', 100)),
                            const SizedBox(width: 12),
                            Expanded(child: _optionalField(_diet, 'Diet', 100)),
                          ],
                        ),
                        const SizedBox(height: 12),
                        Row(
                          children: [
                            Expanded(child: _weightField()),
                            const SizedBox(width: 12),
                            Expanded(child: _optionalField(_lifespan, 'Lifespan', 50)),
                          ],
                        ),
                        const SizedBox(height: 12),
                        _adoptionField(),
                        const SizedBox(height: 12),
                        _optionalField(_imageUrl, 'Image URL', 500),
                        const SizedBox(height: 12),
                        TextFormField(
                          controller: _description,
                          maxLines: 3,
                          maxLength: 1000,
                          decoration: const InputDecoration(
                            labelText: 'Description',
                            alignLabelWithHint: true,
                          ),
                        ),
                        SwitchListTile(
                          contentPadding: EdgeInsets.zero,
                          title: const Text('Active'),
                          value: _isActive,
                          onChanged: (value) => setState(() => _isActive = value),
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
                      onPressed: _saving ? null : () => Navigator.pop(context, false),
                      child: const Text('Cancel'),
                    ),
                    const SizedBox(width: 8),
                    FilledButton(
                      onPressed: _saving || _loadingCategories ? null : _save,
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

  Widget _speciesField() {
    return TextFormField(
      controller: _species,
      decoration: const InputDecoration(labelText: 'Species'),
      validator: (value) {
        final text = value?.trim() ?? '';
        if (text.isEmpty) return 'Species is required.';
        if (text.length > 100) return 'Species cannot be longer than 100 characters.';
        return null;
      },
    );
  }

  Widget _categoryField() {
    if (_loadingCategories) {
      return const InputDecorator(
        decoration: InputDecoration(labelText: 'Category'),
        child: Text('Loading...'),
      );
    }

    final selected = _categories.any((c) => c.id == _categoryId) ? _categoryId : null;
    return DropdownButtonFormField<int>(
      initialValue: selected,
      decoration: const InputDecoration(labelText: 'Category'),
      items: [
        for (final category in _categories)
          DropdownMenuItem(value: category.id, child: Text(category.name)),
      ],
      onChanged: (value) => setState(() => _categoryId = value),
      validator: (value) => value == null ? 'Category is required.' : null,
    );
  }

  Widget _tagField() {
    return DropdownButtonFormField<AnimalTag>(
      initialValue: _tag,
      decoration: const InputDecoration(labelText: 'Tag'),
      items: [
        for (final tag in AnimalTag.values)
          DropdownMenuItem(value: tag, child: Text(animalTagLabel(tag))),
      ],
      onChanged: (value) {
        if (value != null) setState(() => _tag = value);
      },
    );
  }

  Widget _optionalField(TextEditingController controller, String label, int max) {
    return TextFormField(
      controller: controller,
      decoration: InputDecoration(labelText: label),
      validator: (value) {
        if ((value?.trim().length ?? 0) > max) {
          return '$label cannot be longer than $max characters.';
        }
        return null;
      },
    );
  }

  Widget _weightField() {
    return TextFormField(
      controller: _weight,
      decoration: const InputDecoration(labelText: 'Weight (kg)'),
      keyboardType: const TextInputType.numberWithOptions(decimal: true),
      inputFormatters: [FilteringTextInputFormatter.allow(RegExp(r'[0-9.,]'))],
      validator: (value) {
        final parsed = _parseNumber(value ?? '');
        if (value != null && value.trim().isNotEmpty && parsed == null) {
          return 'Enter a valid number.';
        }
        if (parsed != null && parsed < 0) return 'Weight cannot be negative.';
        return null;
      },
    );
  }

  Widget _adoptionField() {
    return TextFormField(
      controller: _adoptionPrice,
      decoration: const InputDecoration(labelText: 'Adoption price (KM)'),
      keyboardType: const TextInputType.numberWithOptions(decimal: true),
      inputFormatters: [FilteringTextInputFormatter.allow(RegExp(r'[0-9.,]'))],
      validator: (value) {
        final parsed = _parseNumber(value ?? '');
        if (value != null && value.trim().isNotEmpty && parsed == null) {
          return 'Enter a valid number.';
        }
        if (parsed != null && parsed < 0) {
          return 'Adoption price cannot be negative.';
        }
        return null;
      },
    );
  }
}

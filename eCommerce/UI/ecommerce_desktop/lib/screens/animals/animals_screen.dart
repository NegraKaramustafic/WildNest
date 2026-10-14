import 'dart:async';

import 'package:ecommerce_desktop/models/animal.dart';
import 'package:ecommerce_desktop/providers/animal_provider.dart';
import 'package:ecommerce_desktop/screens/animals/animal_categories_screen.dart';
import 'package:ecommerce_desktop/screens/animals/animal_form_dialog.dart';
import 'package:ecommerce_desktop/theme/app_colors.dart';
import 'package:ecommerce_desktop/utils/api_client_exception.dart';
import 'package:ecommerce_desktop/utils/formatters.dart';
import 'package:ecommerce_desktop/widgets/status_chip.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

class AnimalsScreen extends StatefulWidget {
  const AnimalsScreen({super.key});

  @override
  State<AnimalsScreen> createState() => _AnimalsScreenState();
}

class _AnimalsScreenState extends State<AnimalsScreen> {
  final _searchController = TextEditingController();
  Timer? _debounce;
  int _requestId = 0;

  bool _loading = true;
  bool _showCategories = false;
  String? _error;
  List<Animal> _animals = [];

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
    final requestId = ++_requestId;
    setState(() {
      _loading = true;
      _error = null;
    });

    try {
      final result = await context.read<AnimalProvider>().get(
        filter: {
          'FTS': _searchController.text.trim(),
          'Page': 1,
          'PageSize': 100,
          'IncludeTotalCount': true,
        },
      );
      if (!mounted || requestId != _requestId) return;
      setState(() {
        _animals = result.items ?? [];
        _loading = false;
      });
    } on SessionExpiredException {
      return;
    } catch (e) {
      if (!mounted || requestId != _requestId) return;
      setState(() {
        _error = _message(e);
        _loading = false;
      });
    }
  }

  void _onSearchChanged(String value) {
    _debounce?.cancel();
    _debounce = Timer(const Duration(milliseconds: 300), _load);
  }

  Future<void> _add() async {
    final saved = await showAnimalFormDialog(context);
    if (saved && mounted) await _load();
  }

  Future<void> _edit(Animal animal) async {
    final saved = await showAnimalFormDialog(context, animal: animal);
    if (saved && mounted) await _load();
  }

  Future<void> _delete(Animal animal) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Brisanje životinje'),
        content: Text('Obrisati životinju ${animal.name}?'),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: const Text('Odustani'),
          ),
          FilledButton(
            style: FilledButton.styleFrom(backgroundColor: AppColors.danger),
            onPressed: () => Navigator.pop(context, true),
            child: const Text('Obriši'),
          ),
        ],
      ),
    );
    if (confirmed != true || !mounted) return;

    try {
      await context.read<AnimalProvider>().remove(animal.id);
      if (mounted) await _load();
    } on SessionExpiredException {
      return;
    } catch (e) {
      if (!mounted) return;
      _showError(_message(e));
    }
  }

  void _showError(String message) {
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(message)));
  }

  String _message(Object error) {
    final text = error.toString();
    return text.replaceFirst('Bad state: ', '');
  }

  @override
  Widget build(BuildContext context) {
    if (_showCategories) {
      return AnimalCategoriesScreen(
        onBack: () => setState(() => _showCategories = false),
      );
    }

    return Padding(
      padding: const EdgeInsets.all(28),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      'Upravljanje životinjama',
                      style: TextStyle(
                        fontSize: 26,
                        fontWeight: FontWeight.w700,
                        color: AppColors.text,
                      ),
                    ),
                    SizedBox(height: 4),
                    Text(
                      'Pregled i uređivanje životinja',
                      style: TextStyle(color: AppColors.textMuted),
                    ),
                  ],
                ),
              ),
              OutlinedButton.icon(
                onPressed: () => setState(() => _showCategories = true),
                icon: const Icon(Icons.category_outlined, size: 18),
                label: const Text('Kategorije'),
              ),
              const SizedBox(width: 10),
              FilledButton.icon(
                onPressed: _add,
                icon: const Icon(Icons.add, size: 18),
                label: const Text('Dodaj životinju'),
              ),
            ],
          ),
          const SizedBox(height: 18),
          SizedBox(
            width: 360,
            child: TextField(
              controller: _searchController,
              onChanged: _onSearchChanged,
              decoration: const InputDecoration(
                hintText: 'Pretraži životinje...',
                prefixIcon: Icon(Icons.search),
                isDense: true,
              ),
            ),
          ),
          const SizedBox(height: 16),
          Expanded(child: _buildBody()),
        ],
      ),
    );
  }

  Widget _buildBody() {
    if (_loading) {
      return const Center(child: CircularProgressIndicator());
    }
    if (_error != null) {
      return _ErrorState(message: _error!, onRetry: _load);
    }
    return _AnimalsTable(
      animals: _animals,
      onEdit: _edit,
      onDelete: _delete,
    );
  }
}

class _AnimalsTable extends StatelessWidget {
  const _AnimalsTable({
    required this.animals,
    required this.onEdit,
    required this.onDelete,
  });

  final List<Animal> animals;
  final ValueChanged<Animal> onEdit;
  final ValueChanged<Animal> onDelete;

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: BoxDecoration(
        color: AppColors.surface,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: AppColors.border),
      ),
      child: Column(
        children: [
          const _HeaderRow(),
          const Divider(height: 1),
          Expanded(
            child: animals.isEmpty
                ? const Center(
                    child: Text(
                      'Nema životinja za prikaz.',
                      style: TextStyle(color: AppColors.textMuted),
                    ),
                  )
                : ListView.separated(
                    itemCount: animals.length,
                    separatorBuilder: (_, _) => const Divider(height: 1),
                    itemBuilder: (context, index) {
                      final animal = animals[index];
                      return _AnimalRow(
                        animal: animal,
                        onEdit: () => onEdit(animal),
                        onDelete: () => onDelete(animal),
                      );
                    },
                  ),
          ),
        ],
      ),
    );
  }
}

class _HeaderRow extends StatelessWidget {
  const _HeaderRow();

  @override
  Widget build(BuildContext context) {
    return const Padding(
      padding: EdgeInsets.symmetric(horizontal: 18, vertical: 14),
      child: Row(
        children: [
          Expanded(flex: 3, child: _HeaderLabel('Životinja')),
          Expanded(flex: 3, child: _HeaderLabel('Vrsta')),
          Expanded(flex: 3, child: _HeaderLabel('Kategorija')),
          Expanded(flex: 2, child: _HeaderLabel('Status')),
          Expanded(flex: 2, child: _HeaderLabel('Usvajanje')),
          SizedBox(width: 96, child: _HeaderLabel('Akcije')),
        ],
      ),
    );
  }
}

class _HeaderLabel extends StatelessWidget {
  const _HeaderLabel(this.text);

  final String text;

  @override
  Widget build(BuildContext context) {
    return Text(
      text.toUpperCase(),
      style: const TextStyle(
        fontSize: 11,
        letterSpacing: 0.6,
        fontWeight: FontWeight.w600,
        color: AppColors.textMuted,
      ),
    );
  }
}

class _AnimalRow extends StatelessWidget {
  const _AnimalRow({
    required this.animal,
    required this.onEdit,
    required this.onDelete,
  });

  final Animal animal;
  final VoidCallback onEdit;
  final VoidCallback onDelete;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 18, vertical: 12),
      child: Row(
        children: [
          Expanded(
            flex: 3,
            child: Text(
              animal.name,
              style: const TextStyle(fontWeight: FontWeight.w600),
            ),
          ),
          Expanded(flex: 3, child: Text(animal.species)),
          Expanded(flex: 3, child: Text(animal.categoryName)),
          Expanded(flex: 2, child: Align(
            alignment: Alignment.centerLeft,
            child: StatusChip(active: animal.isActive),
          )),
          Expanded(flex: 2, child: Text(formatAdoption(animal.adoptionPrice))),
          SizedBox(
            width: 96,
            child: Row(
              children: [
                _IconAction(
                  icon: Icons.edit_outlined,
                  color: AppColors.textMuted,
                  tooltip: 'Uredi',
                  onPressed: onEdit,
                ),
                const SizedBox(width: 6),
                _IconAction(
                  icon: Icons.delete_outline,
                  color: AppColors.danger,
                  tooltip: 'Obriši',
                  onPressed: onDelete,
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _IconAction extends StatelessWidget {
  const _IconAction({
    required this.icon,
    required this.color,
    required this.tooltip,
    required this.onPressed,
  });

  final IconData icon;
  final Color color;
  final String tooltip;
  final VoidCallback onPressed;

  @override
  Widget build(BuildContext context) {
    return Tooltip(
      message: tooltip,
      child: InkWell(
        onTap: onPressed,
        borderRadius: BorderRadius.circular(8),
        child: Container(
          width: 34,
          height: 34,
          decoration: BoxDecoration(
            borderRadius: BorderRadius.circular(8),
            border: Border.all(color: AppColors.border),
          ),
          child: Icon(icon, size: 18, color: color),
        ),
      ),
    );
  }
}

class _ErrorState extends StatelessWidget {
  const _ErrorState({required this.message, required this.onRetry});

  final String message;
  final VoidCallback onRetry;

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Text(message, textAlign: TextAlign.center),
          const SizedBox(height: 12),
          OutlinedButton(onPressed: onRetry, child: const Text('Pokušaj ponovo')),
        ],
      ),
    );
  }
}

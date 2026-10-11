import 'dart:async';

import 'package:ecommerce_desktop/models/animal_category.dart';
import 'package:ecommerce_desktop/providers/animal_category_provider.dart';
import 'package:ecommerce_desktop/theme/app_colors.dart';
import 'package:ecommerce_desktop/utils/api_client_exception.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

class AnimalCategoriesScreen extends StatefulWidget {
  const AnimalCategoriesScreen({super.key, required this.onBack});

  final VoidCallback onBack;

  @override
  State<AnimalCategoriesScreen> createState() => _AnimalCategoriesScreenState();
}

class _AnimalCategoriesScreenState extends State<AnimalCategoriesScreen> {
  final _searchController = TextEditingController();
  Timer? _debounce;
  int _requestId = 0;
  bool _loading = true;
  String? _error;
  List<AnimalCategory> _categories = [];

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
      final result = await context.read<AnimalCategoryProvider>().get(
        filter: {
          'Name': _searchController.text.trim(),
          'Page': 1,
          'PageSize': 200,
          'IncludeTotalCount': true,
        },
      );
      if (!mounted || requestId != _requestId) return;
      setState(() {
        _categories = result.items ?? [];
        _loading = false;
      });
    } on SessionExpiredException {
      return;
    } catch (e) {
      if (!mounted || requestId != _requestId) return;
      setState(() {
        _error = e.toString().replaceFirst('Bad state: ', '');
        _loading = false;
      });
    }
  }

  Future<void> _edit({AnimalCategory? category}) async {
    final name = await showDialog<String>(
      context: context,
      builder: (_) => _CategoryNameDialog(initial: category?.name ?? ''),
    );
    if (name == null || !mounted) return;

    try {
      final provider = context.read<AnimalCategoryProvider>();
      if (category == null) {
        await provider.insert({'name': name});
      } else {
        await provider.update(category.id, {'name': name});
      }
      if (mounted) await _load();
    } on SessionExpiredException {
      return;
    } catch (e) {
      if (!mounted) return;
      _toast(e.toString().replaceFirst('Bad state: ', ''));
    }
  }

  Future<void> _delete(AnimalCategory category) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Brisanje kategorije'),
        content: Text('Obrisati kategoriju ${category.name}?'),
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
      await context.read<AnimalCategoryProvider>().remove(category.id);
      if (mounted) await _load();
    } on SessionExpiredException {
      return;
    } catch (e) {
      if (!mounted) return;
      _toast(e.toString().replaceFirst('Bad state: ', ''));
    }
  }

  void _toast(String message) {
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(message)));
  }

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.all(28),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              IconButton(
                tooltip: 'Nazad',
                onPressed: widget.onBack,
                icon: const Icon(Icons.arrow_back),
              ),
              const SizedBox(width: 4),
              const Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      'Kategorije životinja',
                      style: TextStyle(fontSize: 26, fontWeight: FontWeight.w700),
                    ),
                    SizedBox(height: 4),
                    Text(
                      'Šifarnik kategorija za obrazac životinje',
                      style: TextStyle(color: AppColors.textMuted),
                    ),
                  ],
                ),
              ),
              FilledButton.icon(
                onPressed: () => _edit(),
                icon: const Icon(Icons.add, size: 18),
                label: const Text('Dodaj kategoriju'),
              ),
            ],
          ),
          const SizedBox(height: 18),
          SizedBox(
            width: 360,
            child: TextField(
              controller: _searchController,
              onChanged: (_) {
                _debounce?.cancel();
                _debounce = Timer(const Duration(milliseconds: 300), _load);
              },
              decoration: const InputDecoration(
                hintText: 'Pretraži kategorije...',
                prefixIcon: Icon(Icons.search),
                isDense: true,
              ),
            ),
          ),
          const SizedBox(height: 16),
          Expanded(child: _body()),
        ],
      ),
    );
  }

  Widget _body() {
    if (_loading) return const Center(child: CircularProgressIndicator());
    if (_error != null) {
      return Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Text(_error!, textAlign: TextAlign.center),
            const SizedBox(height: 12),
            OutlinedButton(onPressed: _load, child: const Text('Pokušaj ponovo')),
          ],
        ),
      );
    }

    return Container(
      decoration: BoxDecoration(
        color: AppColors.surface,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: AppColors.border),
      ),
      child: Column(
        children: [
          const Padding(
            padding: EdgeInsets.symmetric(horizontal: 18, vertical: 14),
            child: Row(
              children: [
                Expanded(
                  child: Text(
                    'NAZIV',
                    style: TextStyle(
                      fontSize: 11,
                      letterSpacing: 0.6,
                      fontWeight: FontWeight.w600,
                      color: AppColors.textMuted,
                    ),
                  ),
                ),
                SizedBox(
                  width: 96,
                  child: Text(
                    'AKCIJE',
                    style: TextStyle(
                      fontSize: 11,
                      letterSpacing: 0.6,
                      fontWeight: FontWeight.w600,
                      color: AppColors.textMuted,
                    ),
                  ),
                ),
              ],
            ),
          ),
          const Divider(height: 1),
          Expanded(
            child: _categories.isEmpty
                ? const Center(
                    child: Text(
                      'Nema kategorija.',
                      style: TextStyle(color: AppColors.textMuted),
                    ),
                  )
                : ListView.separated(
                    itemCount: _categories.length,
                    separatorBuilder: (_, _) => const Divider(height: 1),
                    itemBuilder: (context, index) {
                      final category = _categories[index];
                      return Padding(
                        padding: const EdgeInsets.symmetric(horizontal: 18, vertical: 10),
                        child: Row(
                          children: [
                            Expanded(child: Text(category.name)),
                            SizedBox(
                              width: 96,
                              child: Row(
                                children: [
                                  IconButton(
                                    tooltip: 'Uredi',
                                    onPressed: () => _edit(category: category),
                                    icon: const Icon(Icons.edit_outlined, size: 18),
                                  ),
                                  IconButton(
                                    tooltip: 'Obriši',
                                    onPressed: () => _delete(category),
                                    icon: const Icon(
                                      Icons.delete_outline,
                                      size: 18,
                                      color: AppColors.danger,
                                    ),
                                  ),
                                ],
                              ),
                            ),
                          ],
                        ),
                      );
                    },
                  ),
          ),
        ],
      ),
    );
  }
}

class _CategoryNameDialog extends StatefulWidget {
  const _CategoryNameDialog({required this.initial});

  final String initial;

  @override
  State<_CategoryNameDialog> createState() => _CategoryNameDialogState();
}

class _CategoryNameDialogState extends State<_CategoryNameDialog> {
  late final TextEditingController _controller;
  final _formKey = GlobalKey<FormState>();

  @override
  void initState() {
    super.initState();
    _controller = TextEditingController(text: widget.initial);
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final isEdit = widget.initial.isNotEmpty;
    return AlertDialog(
      title: Text(isEdit ? 'Uredi kategoriju' : 'Nova kategorija'),
      content: Form(
        key: _formKey,
        child: TextFormField(
          controller: _controller,
          autofocus: true,
          decoration: const InputDecoration(labelText: 'Naziv'),
          validator: (value) {
            final text = value?.trim() ?? '';
            if (text.isEmpty) return 'Naziv je obavezan.';
            if (text.length > 50) return 'Naziv ne može biti duži od 50 znakova.';
            return null;
          },
        ),
      ),
      actions: [
        TextButton(
          onPressed: () => Navigator.pop(context),
          child: const Text('Odustani'),
        ),
        FilledButton(
          onPressed: () {
            if (_formKey.currentState!.validate()) {
              Navigator.pop(context, _controller.text.trim());
            }
          },
          child: const Text('Sačuvaj'),
        ),
      ],
    );
  }
}

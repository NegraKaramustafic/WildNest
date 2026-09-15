import 'dart:async';

import 'package:ecommerce_mobile/models/animal.dart';
import 'package:ecommerce_mobile/models/animal_category.dart';
import 'package:ecommerce_mobile/providers/animal_category_provider.dart';
import 'package:ecommerce_mobile/providers/animal_provider.dart';
import 'package:ecommerce_mobile/screens/animals/animal_detail_screen.dart';
import 'package:ecommerce_mobile/theme/app_colors.dart';
import 'package:ecommerce_mobile/utils/utils_widgets.dart';
import 'package:ecommerce_mobile/widgets/animal_card.dart';
import 'package:ecommerce_mobile/widgets/filter_chip_row.dart';
import 'package:ecommerce_mobile/widgets/wildnest_header.dart';
import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:provider/provider.dart';

class AnimalsListScreen extends StatefulWidget {
  const AnimalsListScreen({super.key});

  @override
  State<AnimalsListScreen> createState() => _AnimalsListScreenState();
}

class _AnimalsListScreenState extends State<AnimalsListScreen> {
  static const _statusOptions = <_StatusFilter>[
    _StatusFilter('All', null),
    _StatusFilter('Adoptable', AnimalTag.adoptable),
    _StatusFilter('Popular', AnimalTag.popular),
    _StatusFilter('New', AnimalTag.isNew),
  ];

  final _searchController = TextEditingController();
  Timer? _debounce;

  List<AnimalCategory> _categories = [];
  List<Animal> _animals = [];
  int _statusIndex = 0;
  int _categoryIndex = 0;
  bool _loading = true;
  String? _error;
  int _requestId = 0;

  @override
  void initState() {
    super.initState();
    _searchController.addListener(_onSearchChanged);
    WidgetsBinding.instance.addPostFrameCallback((_) {
      _loadCategories();
      _loadAnimals();
    });
  }

  @override
  void dispose() {
    _debounce?.cancel();
    _searchController.dispose();
    super.dispose();
  }

  void _onSearchChanged() {
    _debounce?.cancel();
    _debounce = Timer(const Duration(milliseconds: 400), _loadAnimals);
  }

  Future<void> _loadCategories() async {
    try {
      final result = await context.read<AnimalCategoryProvider>().get(
        filter: {'page': 1, 'pageSize': 50, 'includeTotalCount': false},
      );
      if (!mounted) return;
      setState(() {
        _categories = result.items ?? [];
      });
    } catch (e) {
      if (!mounted) return;
      showAppError(context, e);
    }
  }

  Future<void> _loadAnimals() async {
    final requestId = ++_requestId;
    setState(() {
      _loading = true;
      _error = null;
    });

    try {
      final filter = <String, dynamic>{
        'page': 1,
        'pageSize': 50,
        'includeTotalCount': true,
      };

      final query = _searchController.text.trim();
      if (query.isNotEmpty) {
        filter['FTS'] = query;
      }

      final status = _statusOptions[_statusIndex].tag;
      if (status != null) {
        filter['Tag'] = animalTagQueryValue(status);
      }

      if (_categoryIndex > 0 && _categoryIndex <= _categories.length) {
        filter['CategoryId'] = _categories[_categoryIndex - 1].id;
      }

      final result = await context.read<AnimalProvider>().get(filter: filter);
      if (!mounted || requestId != _requestId) return;
      setState(() {
        _animals = result.items ?? [];
        _loading = false;
      });
    } catch (e) {
      if (!mounted || requestId != _requestId) return;
      setState(() {
        _loading = false;
        _error = e.toString();
      });
      showAppError(context, e);
    }
  }

  List<String> get _categoryLabels => [
    'All',
    ..._categories.map((c) => c.name),
  ];

  @override
  Widget build(BuildContext context) {
    return SafeArea(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const WildNestHeader(),
          Padding(
            padding: const EdgeInsets.fromLTRB(20, 8, 20, 0),
            child: Text(
              'Our Animals',
              style: GoogleFonts.playfairDisplay(
                fontSize: 32,
                fontWeight: FontWeight.w600,
              ),
            ),
          ),
          Padding(
            padding: const EdgeInsets.fromLTRB(20, 14, 20, 12),
            child: TextField(
              controller: _searchController,
              decoration: InputDecoration(
                hintText: 'Search animals...',
                prefixIcon: const Icon(Icons.search, color: AppColors.textMuted),
                filled: true,
                fillColor: AppColors.surface,
                contentPadding: const EdgeInsets.symmetric(vertical: 14),
                border: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(28),
                  borderSide: BorderSide.none,
                ),
                enabledBorder: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(28),
                  borderSide: BorderSide.none,
                ),
                focusedBorder: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(28),
                  borderSide: const BorderSide(color: AppColors.sage),
                ),
              ),
            ),
          ),
          FilterChipRow(
            labels: _statusOptions.map((e) => e.label).toList(),
            selectedIndex: _statusIndex,
            onSelected: (index) {
              setState(() => _statusIndex = index);
              _loadAnimals();
            },
          ),
          const SizedBox(height: 10),
          FilterChipRow(
            labels: _categoryLabels,
            selectedIndex: _categoryIndex,
            onSelected: (index) {
              setState(() => _categoryIndex = index);
              _loadAnimals();
            },
          ),
          const SizedBox(height: 12),
          Expanded(child: _body()),
        ],
      ),
    );
  }

  Widget _body() {
    if (_loading) {
      return const Center(
        child: CircularProgressIndicator(color: AppColors.sage),
      );
    }
    if (_error != null && _animals.isEmpty) {
      return Center(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Text(
                _error!,
                textAlign: TextAlign.center,
                style: const TextStyle(color: AppColors.textMuted),
              ),
              const SizedBox(height: 12),
              ElevatedButton(
                onPressed: _loadAnimals,
                child: const Text('Try again'),
              ),
            ],
          ),
        ),
      );
    }
    if (_animals.isEmpty) {
      return const Center(
        child: Text(
          'No animals match these filters.',
          style: TextStyle(color: AppColors.textMuted),
        ),
      );
    }

    return RefreshIndicator(
      color: AppColors.sage,
      onRefresh: _loadAnimals,
      child: GridView.builder(
        padding: const EdgeInsets.fromLTRB(16, 4, 16, 88),
        gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
          crossAxisCount: 2,
          mainAxisSpacing: 12,
          crossAxisSpacing: 12,
          childAspectRatio: 0.70,
        ),
        itemCount: _animals.length,
        itemBuilder: (context, index) {
          final animal = _animals[index];
          return AnimalCard(
            animal: animal,
            onTap: () {
              Navigator.of(context).push(
                MaterialPageRoute(
                  builder: (_) => AnimalDetailScreen(animalId: animal.id),
                ),
              );
            },
          );
        },
      ),
    );
  }
}

class _StatusFilter {
  const _StatusFilter(this.label, this.tag);

  final String label;
  final AnimalTag? tag;
}

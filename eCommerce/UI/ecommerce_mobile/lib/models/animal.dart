enum AnimalTag { none, popular, isNew, adoptable }

class Animal {
  final int id;
  final String name;
  final String species;
  final int categoryId;
  final String categoryName;
  final String? habitat;
  final String? diet;
  final double weight;
  final String? lifespan;
  final String? description;
  final String? imageUrl;
  final AnimalTag tag;
  final bool isActive;
  final double? adoptionPrice;
  final DateTime? createdAt;

  Animal({
    required this.id,
    required this.name,
    required this.species,
    required this.categoryId,
    required this.categoryName,
    this.habitat,
    this.diet,
    this.weight = 0,
    this.lifespan,
    this.description,
    this.imageUrl,
    this.tag = AnimalTag.none,
    this.isActive = true,
    this.adoptionPrice,
    this.createdAt,
  });

  factory Animal.fromJson(Map<String, dynamic> json) {
    return Animal(
      id: (json['id'] as num).toInt(),
      name: json['name'] as String? ?? '',
      species: json['species'] as String? ?? '',
      categoryId: (json['categoryId'] as num?)?.toInt() ?? 0,
      categoryName: json['categoryName'] as String? ?? '',
      habitat: json['habitat'] as String?,
      diet: json['diet'] as String?,
      weight: (json['weight'] as num?)?.toDouble() ?? 0,
      lifespan: json['lifespan'] as String?,
      description: json['description'] as String?,
      imageUrl: json['imageUrl'] as String?,
      tag: parseAnimalTag(json['tag']),
      isActive: json['isActive'] as bool? ?? true,
      adoptionPrice: (json['adoptionPrice'] as num?)?.toDouble(),
      createdAt: json['createdAt'] == null
          ? null
          : DateTime.tryParse(json['createdAt'] as String),
    );
  }

  String get tagLabel {
    switch (tag) {
      case AnimalTag.adoptable:
        return 'Adoptable';
      case AnimalTag.popular:
        return 'Popular';
      case AnimalTag.isNew:
        return 'New';
      case AnimalTag.none:
        return '';
    }
  }

  String get apiTagValue {
    switch (tag) {
      case AnimalTag.adoptable:
        return 'Adoptable';
      case AnimalTag.popular:
        return 'Popular';
      case AnimalTag.isNew:
        return 'New';
      case AnimalTag.none:
        return 'None';
    }
  }
}

AnimalTag parseAnimalTag(dynamic value) {
  if (value is int) {
    switch (value) {
      case 1:
        return AnimalTag.popular;
      case 2:
        return AnimalTag.isNew;
      case 3:
        return AnimalTag.adoptable;
      default:
        return AnimalTag.none;
    }
  }

  switch (value?.toString().toLowerCase()) {
    case 'popular':
      return AnimalTag.popular;
    case 'new':
      return AnimalTag.isNew;
    case 'adoptable':
      return AnimalTag.adoptable;
    default:
      return AnimalTag.none;
  }
}

String animalTagQueryValue(AnimalTag tag) {
  switch (tag) {
    case AnimalTag.adoptable:
      return 'Adoptable';
    case AnimalTag.popular:
      return 'Popular';
    case AnimalTag.isNew:
      return 'New';
    case AnimalTag.none:
      return 'None';
  }
}

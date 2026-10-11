import 'package:ecommerce_desktop/utils/json_parsers.dart';

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
      id: asInt(json['id']),
      name: asString(json['name']),
      species: asString(json['species']),
      categoryId: asInt(json['categoryId']),
      categoryName: asString(json['categoryName']),
      habitat: json['habitat'] as String?,
      diet: json['diet'] as String?,
      weight: asDouble(json['weight']),
      lifespan: json['lifespan'] as String?,
      description: json['description'] as String?,
      imageUrl: json['imageUrl'] as String?,
      tag: parseAnimalTag(json['tag']),
      isActive: json['isActive'] as bool? ?? true,
      adoptionPrice: asDoubleOrNull(json['adoptionPrice']),
      createdAt: parseDateTime(json['createdAt']),
    );
  }

  String get apiTagValue => animalTagApiValue(tag);
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

String animalTagApiValue(AnimalTag tag) {
  switch (tag) {
    case AnimalTag.popular:
      return 'Popular';
    case AnimalTag.isNew:
      return 'New';
    case AnimalTag.adoptable:
      return 'Adoptable';
    case AnimalTag.none:
      return 'None';
  }
}

String animalTagLabel(AnimalTag tag) {
  switch (tag) {
    case AnimalTag.popular:
      return 'Popularno';
    case AnimalTag.isNew:
      return 'Novo';
    case AnimalTag.adoptable:
      return 'Za usvajanje';
    case AnimalTag.none:
      return 'Bez oznake';
  }
}

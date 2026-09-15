class AnimalCategory {
  final int id;
  final String name;

  AnimalCategory({required this.id, required this.name});

  factory AnimalCategory.fromJson(Map<String, dynamic> json) {
    return AnimalCategory(
      id: (json['id'] as num).toInt(),
      name: json['name'] as String? ?? '',
    );
  }
}

import 'package:ecommerce_mobile/models/animal_category.dart';
import 'package:ecommerce_mobile/providers/base_provider.dart';

class AnimalCategoryProvider extends BaseProvider<AnimalCategory> {
  AnimalCategoryProvider() : super('api/AnimalCategory');

  @override
  AnimalCategory fromJson(data) {
    return AnimalCategory.fromJson(data as Map<String, dynamic>);
  }
}

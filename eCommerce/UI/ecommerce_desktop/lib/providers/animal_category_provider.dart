import 'package:ecommerce_desktop/models/animal_category.dart';
import 'package:ecommerce_desktop/providers/base_provider.dart';

class AnimalCategoryProvider extends BaseProvider<AnimalCategory> {
  AnimalCategoryProvider() : super('api/AnimalCategory');

  @override
  AnimalCategory fromJson(data) {
    return AnimalCategory.fromJson(data as Map<String, dynamic>);
  }
}

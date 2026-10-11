import 'package:ecommerce_desktop/models/animal.dart';
import 'package:ecommerce_desktop/providers/base_provider.dart';

class AnimalProvider extends BaseProvider<Animal> {
  AnimalProvider() : super('api/Animal');

  @override
  Animal fromJson(data) {
    return Animal.fromJson(data as Map<String, dynamic>);
  }
}

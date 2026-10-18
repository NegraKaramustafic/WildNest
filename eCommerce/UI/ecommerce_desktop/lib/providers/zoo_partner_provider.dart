import 'package:ecommerce_desktop/models/zoo_partner.dart';
import 'package:ecommerce_desktop/providers/base_provider.dart';

class ZooPartnerProvider extends BaseProvider<ZooPartner> {
  ZooPartnerProvider() : super('api/ZooPartner');

  @override
  ZooPartner fromJson(data) => ZooPartner.fromJson(data as Map<String, dynamic>);
}

import 'package:ecommerce_mobile/models/zoo_partner.dart';
import 'package:ecommerce_mobile/providers/base_provider.dart';

class ZooPartnerProvider extends BaseProvider<ZooPartner> {
  ZooPartnerProvider() : super('api/ZooPartner');

  @override
  ZooPartner fromJson(data) {
    return ZooPartner.fromJson(data as Map<String, dynamic>);
  }

  Future<List<ZooPartner>> getActive({int pageSize = 50}) async {
    final result = await get(
      filter: {
        'page': 1,
        'pageSize': pageSize,
        'includeTotalCount': false,
        'IsActive': true,
      },
    );
    return result.items ?? [];
  }
}

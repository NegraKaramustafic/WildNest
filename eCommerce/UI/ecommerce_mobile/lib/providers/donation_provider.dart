import 'package:ecommerce_mobile/models/donation.dart';
import 'package:ecommerce_mobile/providers/base_provider.dart';

class DonationProvider extends BaseProvider<Donation> {
  DonationProvider() : super('api/Donation');

  @override
  Donation fromJson(data) {
    return Donation.fromJson(data as Map<String, dynamic>);
  }

  Future<Donation> createPending({
    required int zooPartnerId,
    required double amount,
  }) {
    return insert({
      'zooPartnerId': zooPartnerId,
      'amount': amount,
    });
  }
}

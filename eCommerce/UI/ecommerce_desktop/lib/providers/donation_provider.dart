import 'package:ecommerce_desktop/models/donation.dart';
import 'package:ecommerce_desktop/providers/base_provider.dart';

class DonationProvider extends BaseProvider<Donation> {
  DonationProvider() : super('api/Donation');

  @override
  Donation fromJson(data) => Donation.fromJson(data as Map<String, dynamic>);
}

import 'package:ecommerce_mobile/models/reservation.dart';
import 'package:ecommerce_mobile/providers/base_provider.dart';

class ReservationProvider extends BaseProvider<Reservation> {
  ReservationProvider() : super('api/Reservation');

  @override
  Reservation fromJson(data) {
    return Reservation.fromJson(data as Map<String, dynamic>);
  }

  Future<Reservation> createPending({
    required int scheduleId,
    required int guestCount,
  }) {
    return insert({
      'scheduleId': scheduleId,
      'guestCount': guestCount,
    });
  }
}

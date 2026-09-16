import 'package:ecommerce_mobile/utils/json_parsers.dart';

enum NotificationType {
  ticketPurchased,
  ticketVerified,
  reservationCreated,
  reservationConfirmed,
  reservationCancelled,
  adoptionCreated,
  donationReceived,
  paymentCompleted,
  badgeEarned,
}

class AppNotification {
  final int id;
  final int userId;
  final String userFullName;
  final String title;
  final String message;
  final NotificationType type;
  final bool isRead;
  final DateTime createdAt;
  final String? relatedEntityType;
  final int? relatedEntityId;

  AppNotification({
    required this.id,
    required this.userId,
    this.userFullName = '',
    required this.title,
    required this.message,
    this.type = NotificationType.paymentCompleted,
    this.isRead = false,
    required this.createdAt,
    this.relatedEntityType,
    this.relatedEntityId,
  });

  factory AppNotification.fromJson(Map<String, dynamic> json) {
    return AppNotification(
      id: asInt(json['id']),
      userId: asInt(json['userId']),
      userFullName: asString(json['userFullName']),
      title: asString(json['title']),
      message: asString(json['message']),
      type: parseNotificationType(json['type']),
      isRead: json['isRead'] as bool? ?? false,
      createdAt: parseDateTime(json['createdAt']) ?? DateTime.now(),
      relatedEntityType: json['relatedEntityType'] as String?,
      relatedEntityId: json['relatedEntityId'] == null
          ? null
          : asInt(json['relatedEntityId']),
    );
  }

  AppNotification copyWith({bool? isRead}) {
    return AppNotification(
      id: id,
      userId: userId,
      userFullName: userFullName,
      title: title,
      message: message,
      type: type,
      isRead: isRead ?? this.isRead,
      createdAt: createdAt,
      relatedEntityType: relatedEntityType,
      relatedEntityId: relatedEntityId,
    );
  }
}

NotificationType parseNotificationType(dynamic value) {
  if (value is int) {
    switch (value) {
      case 0:
        return NotificationType.ticketPurchased;
      case 1:
        return NotificationType.ticketVerified;
      case 2:
        return NotificationType.reservationCreated;
      case 3:
        return NotificationType.reservationConfirmed;
      case 4:
        return NotificationType.reservationCancelled;
      case 5:
        return NotificationType.adoptionCreated;
      case 6:
        return NotificationType.donationReceived;
      case 7:
        return NotificationType.paymentCompleted;
      case 8:
        return NotificationType.badgeEarned;
      default:
        return NotificationType.paymentCompleted;
    }
  }
  switch (value?.toString().toLowerCase()) {
    case 'ticketpurchased':
      return NotificationType.ticketPurchased;
    case 'ticketverified':
      return NotificationType.ticketVerified;
    case 'reservationcreated':
      return NotificationType.reservationCreated;
    case 'reservationconfirmed':
      return NotificationType.reservationConfirmed;
    case 'reservationcancelled':
      return NotificationType.reservationCancelled;
    case 'adoptioncreated':
      return NotificationType.adoptionCreated;
    case 'donationreceived':
      return NotificationType.donationReceived;
    case 'badgeearned':
      return NotificationType.badgeEarned;
    default:
      return NotificationType.paymentCompleted;
  }
}

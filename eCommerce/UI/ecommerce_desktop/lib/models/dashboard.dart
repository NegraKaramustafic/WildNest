import 'package:ecommerce_desktop/utils/json_parsers.dart';

class DashboardSnapshot {
  final int reservationsToday;
  final int reservationsThisMonth;
  final double ticketRevenueThisMonth;
  final double donationRevenueThisMonth;
  final double revenueThisMonth;
  final int activeSchedules;
  final int newUsersThisMonth;
  final double? averageRating;
  final int totalVisitors;
  final int totalAnimals;
  final int totalReservations;
  final int completedDonationCount;
  final double completedDonationAmount;
  final double totalRevenue;
  final double ticketPaymentTotal;
  final double reservationPaymentTotal;
  final double adoptionPaymentTotal;
  final double donationPaymentTotal;
  final int activeAdoptions;
  final List<DashboardMonth> months;
  final List<DashboardActivity> topActivities;
  final List<DashboardDonationStatus> donationsByStatus;
  final List<DashboardVisitorMonth> visitorMonths;
  final List<DashboardRevenueMonth> revenueMonths;
  final List<DashboardCategoryCount> adoptionsByCategory;
  final List<DashboardActivityItem> recentActivity;

  const DashboardSnapshot({
    required this.reservationsToday,
    required this.reservationsThisMonth,
    required this.ticketRevenueThisMonth,
    required this.donationRevenueThisMonth,
    required this.revenueThisMonth,
    required this.activeSchedules,
    required this.newUsersThisMonth,
    required this.averageRating,
    required this.totalVisitors,
    required this.totalAnimals,
    required this.totalReservations,
    required this.completedDonationCount,
    required this.completedDonationAmount,
    required this.totalRevenue,
    required this.ticketPaymentTotal,
    required this.reservationPaymentTotal,
    required this.adoptionPaymentTotal,
    required this.donationPaymentTotal,
    required this.activeAdoptions,
    required this.months,
    required this.topActivities,
    required this.donationsByStatus,
    required this.visitorMonths,
    required this.revenueMonths,
    required this.adoptionsByCategory,
    required this.recentActivity,
  });

  bool get hasReservationTrend => months.any((month) => month.reservationCount > 0);

  bool get hasRevenueTrend => months.any((month) => month.revenue > 0);

  factory DashboardSnapshot.fromJson(Map<String, dynamic> json) {
    return DashboardSnapshot(
      reservationsToday: asInt(json['reservationsToday']),
      reservationsThisMonth: asInt(json['reservationsThisMonth']),
      ticketRevenueThisMonth: asDouble(json['ticketRevenueThisMonth']),
      donationRevenueThisMonth: asDouble(json['donationRevenueThisMonth']),
      revenueThisMonth: asDouble(json['revenueThisMonth']),
      activeSchedules: asInt(json['activeSchedules']),
      newUsersThisMonth: asInt(json['newUsersThisMonth']),
      averageRating: asDoubleOrNull(json['averageRating']),
      totalVisitors: asInt(json['totalVisitors']),
      totalAnimals: asInt(json['totalAnimals']),
      totalReservations: asInt(json['totalReservations']),
      completedDonationCount: asInt(json['completedDonationCount']),
      completedDonationAmount: asDouble(json['completedDonationAmount']),
      totalRevenue: asDouble(json['totalRevenue']),
      ticketPaymentTotal: asDouble(json['ticketPaymentTotal']),
      reservationPaymentTotal: asDouble(json['reservationPaymentTotal']),
      adoptionPaymentTotal: asDouble(json['adoptionPaymentTotal']),
      donationPaymentTotal: asDouble(json['donationPaymentTotal']),
      activeAdoptions: asInt(json['activeAdoptions']),
      months: [
        for (final item in (json['months'] as List? ?? []))
          DashboardMonth.fromJson(Map<String, dynamic>.from(item as Map)),
      ],
      topActivities: [
        for (final item in (json['topActivities'] as List? ?? []))
          DashboardActivity.fromJson(Map<String, dynamic>.from(item as Map)),
      ],
      donationsByStatus: [
        for (final item in (json['donationsByStatus'] as List? ?? []))
          DashboardDonationStatus.fromJson(Map<String, dynamic>.from(item as Map)),
      ],
      visitorMonths: [
        for (final item in (json['visitorMonths'] as List? ?? []))
          DashboardVisitorMonth.fromJson(Map<String, dynamic>.from(item as Map)),
      ],
      revenueMonths: [
        for (final item in (json['revenueMonths'] as List? ?? []))
          DashboardRevenueMonth.fromJson(Map<String, dynamic>.from(item as Map)),
      ],
      adoptionsByCategory: [
        for (final item in (json['adoptionsByCategory'] as List? ?? []))
          DashboardCategoryCount.fromJson(Map<String, dynamic>.from(item as Map)),
      ],
      recentActivity: [
        for (final item in (json['recentActivity'] as List? ?? []))
          DashboardActivityItem.fromJson(Map<String, dynamic>.from(item as Map)),
      ],
    );
  }
}

class DashboardMonth {
  final int year;
  final int month;
  final int reservationCount;
  final double revenue;

  const DashboardMonth({
    required this.year,
    required this.month,
    required this.reservationCount,
    required this.revenue,
  });

  factory DashboardMonth.fromJson(Map<String, dynamic> json) {
    return DashboardMonth(
      year: asInt(json['year']),
      month: asInt(json['month']),
      reservationCount: asInt(json['reservationCount']),
      revenue: asDouble(json['revenue']),
    );
  }
}

class DashboardActivity {
  final int activityId;
  final String activityName;
  final int reservationCount;
  final double revenue;

  const DashboardActivity({
    required this.activityId,
    required this.activityName,
    required this.reservationCount,
    required this.revenue,
  });

  factory DashboardActivity.fromJson(Map<String, dynamic> json) {
    return DashboardActivity(
      activityId: asInt(json['activityId']),
      activityName: asString(json['activityName']),
      reservationCount: asInt(json['reservationCount']),
      revenue: asDouble(json['revenue']),
    );
  }
}

class DashboardDonationStatus {
  final String status;
  final int count;
  final double amount;

  const DashboardDonationStatus({
    required this.status,
    required this.count,
    required this.amount,
  });

  factory DashboardDonationStatus.fromJson(Map<String, dynamic> json) {
    return DashboardDonationStatus(
      status: asString(json['status']),
      count: asInt(json['count']),
      amount: asDouble(json['amount']),
    );
  }
}

class DashboardVisitorMonth {
  final int year;
  final int month;
  final int visitors;

  const DashboardVisitorMonth({
    required this.year,
    required this.month,
    required this.visitors,
  });

  factory DashboardVisitorMonth.fromJson(Map<String, dynamic> json) {
    return DashboardVisitorMonth(
      year: asInt(json['year']),
      month: asInt(json['month']),
      visitors: asInt(json['visitors']),
    );
  }
}

class DashboardRevenueMonth {
  final int year;
  final int month;
  final double ticketRevenue;
  final double reservationRevenue;
  final double adoptionRevenue;
  final double donationRevenue;
  final double total;

  const DashboardRevenueMonth({
    required this.year,
    required this.month,
    required this.ticketRevenue,
    required this.reservationRevenue,
    required this.adoptionRevenue,
    required this.donationRevenue,
    required this.total,
  });

  factory DashboardRevenueMonth.fromJson(Map<String, dynamic> json) {
    return DashboardRevenueMonth(
      year: asInt(json['year']),
      month: asInt(json['month']),
      ticketRevenue: asDouble(json['ticketRevenue']),
      reservationRevenue: asDouble(json['reservationRevenue']),
      adoptionRevenue: asDouble(json['adoptionRevenue']),
      donationRevenue: asDouble(json['donationRevenue']),
      total: asDouble(json['total']),
    );
  }
}

class DashboardCategoryCount {
  final String categoryName;
  final int count;

  const DashboardCategoryCount({
    required this.categoryName,
    required this.count,
  });

  factory DashboardCategoryCount.fromJson(Map<String, dynamic> json) {
    return DashboardCategoryCount(
      categoryName: asString(json['categoryName']),
      count: asInt(json['count']),
    );
  }
}

class DashboardActivityItem {
  final String type;
  final String label;
  final double? amount;
  final DateTime occurredAtUtc;

  const DashboardActivityItem({
    required this.type,
    required this.label,
    required this.amount,
    required this.occurredAtUtc,
  });

  factory DashboardActivityItem.fromJson(Map<String, dynamic> json) {
    return DashboardActivityItem(
      type: asString(json['type']),
      label: asString(json['label']),
      amount: asDoubleOrNull(json['amount']),
      occurredAtUtc: parseDateTime(json['occurredAtUtc']) ?? DateTime.utc(1970),
    );
  }
}

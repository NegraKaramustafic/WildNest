enum PaymentStatus { pending, completed, failed, refunded }

PaymentStatus? parsePaymentStatus(dynamic value) {
  if (value == null) return null;
  if (value is int) {
    switch (value) {
      case 0:
        return PaymentStatus.pending;
      case 1:
        return PaymentStatus.completed;
      case 2:
        return PaymentStatus.failed;
      case 3:
        return PaymentStatus.refunded;
      default:
        return null;
    }
  }
  switch (value.toString().toLowerCase()) {
    case 'pending':
      return PaymentStatus.pending;
    case 'completed':
      return PaymentStatus.completed;
    case 'failed':
      return PaymentStatus.failed;
    case 'refunded':
      return PaymentStatus.refunded;
    default:
      return null;
  }
}

String paymentStatusLabel(PaymentStatus? status) {
  switch (status) {
    case PaymentStatus.pending:
      return 'Pending';
    case PaymentStatus.completed:
      return 'Completed';
    case PaymentStatus.failed:
      return 'Failed';
    case PaymentStatus.refunded:
      return 'Refunded';
    case null:
      return 'No payment';
  }
}

String paymentStatusApiValue(PaymentStatus status) {
  switch (status) {
    case PaymentStatus.pending:
      return 'Pending';
    case PaymentStatus.completed:
      return 'Completed';
    case PaymentStatus.failed:
      return 'Failed';
    case PaymentStatus.refunded:
      return 'Refunded';
  }
}

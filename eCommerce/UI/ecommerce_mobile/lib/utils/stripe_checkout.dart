import 'dart:async';

import 'package:app_links/app_links.dart';
import 'package:ecommerce_mobile/models/payment.dart';
import 'package:ecommerce_mobile/providers/payment_provider.dart';
import 'package:flutter/material.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:url_launcher/url_launcher.dart';

enum StripeCheckoutOutcome { success, cancelled, dismissed, failedToLaunch }

final wildNestMessengerKey = GlobalKey<ScaffoldMessengerState>();

class StripeReturnLinks {
  static const scheme = 'wildnest';
  static const successHost = 'payment-success';
  static const cancelHost = 'payment-cancel';

  static final instance = StripeReturnLinks._();
  StripeReturnLinks._();

  final _controller = StreamController<Uri>.broadcast();
  Future<void>? _init;
  AppLinks? _links;
  Uri? _initialLink;
  bool _initialTaken = false;

  Stream<Uri> get stream => _controller.stream;

  Future<void> ensureInitialized() {
    return _init ??= _start();
  }

  Future<void> _start() async {
    _links = AppLinks();
    final links = _links!;
    try {
      final initial = await links.getInitialLink();
      if (initial != null && initial.scheme == scheme) {
        _initialLink = initial;
      }
    } catch (_) {}
    links.uriLinkStream.listen((uri) {
      if (uri.scheme == scheme) {
        _controller.add(uri);
      }
    });
  }

  /// Startup link, available until a checkout accepts it.
  Uri? peekInitialLink() => _initialTaken ? null : _initialLink;

  void takeInitialLink() {
    _initialTaken = true;
  }

  static StripeCheckoutOutcome? outcomeFor(Uri uri) {
    if (uri.scheme != scheme) return null;
    switch (uri.host) {
      case successHost:
        return StripeCheckoutOutcome.success;
      case cancelHost:
        return StripeCheckoutOutcome.cancelled;
      default:
        return null;
    }
  }

  static bool successMatches(Uri uri, String? sessionId) {
    if (outcomeFor(uri) != StripeCheckoutOutcome.success) return false;
    final returned = uri.queryParameters['session_id'];
    if (sessionId == null || sessionId.isEmpty) {
      return returned == null || returned.isEmpty;
    }
    return returned == sessionId;
  }
}

class PendingCheckout {
  const PendingCheckout({required this.paymentId, this.sessionId});

  final int paymentId;
  final String? sessionId;
}

class PendingCheckoutStore {
  static const paymentKey = 'wildnest.checkout.paymentId';
  static const sessionKey = 'wildnest.checkout.sessionId';
  static const _storage = FlutterSecureStorage();

  static Future<void> write(int paymentId, String? sessionId) async {
    await _storage.write(key: paymentKey, value: paymentId.toString());
    if (sessionId == null || sessionId.isEmpty) {
      await _storage.delete(key: sessionKey);
    } else {
      await _storage.write(key: sessionKey, value: sessionId);
    }
  }

  static Future<PendingCheckout?> read() async {
    final raw = await _storage.read(key: paymentKey);
    final paymentId = int.tryParse(raw ?? '');
    if (paymentId == null) return null;
    final sessionId = await _storage.read(key: sessionKey);
    return PendingCheckout(
      paymentId: paymentId,
      sessionId: sessionId == null || sessionId.isEmpty ? null : sessionId,
    );
  }

  static Future<void> clear() async {
    await _storage.delete(key: paymentKey);
    await _storage.delete(key: sessionKey);
  }
}

Future<StripeCheckoutOutcome> launchStripeCheckout(
  String url, {
  PaymentProvider? payments,
  int? paymentId,
  String? stripeCheckoutSessionId,
}) async {
  await StripeReturnLinks.instance.ensureInitialized();
  final checkoutUri = Uri.parse(url);
  final completer = Completer<StripeCheckoutOutcome>();
  final openedAt = DateTime.now();
  var successArrived = false;

  void finish(StripeCheckoutOutcome outcome) {
    if (!completer.isCompleted) {
      completer.complete(outcome);
    }
  }

  void accept(Uri uri, {required bool allowCancel}) {
    if (StripeReturnLinks.successMatches(uri, stripeCheckoutSessionId)) {
      successArrived = true;
      finish(StripeCheckoutOutcome.success);
      return;
    }
    if (allowCancel &&
        StripeReturnLinks.outcomeFor(uri) == StripeCheckoutOutcome.cancelled) {
      finish(StripeCheckoutOutcome.cancelled);
    }
  }

  final initial = StripeReturnLinks.instance.peekInitialLink();
  if (initial != null &&
      StripeReturnLinks.successMatches(initial, stripeCheckoutSessionId)) {
    StripeReturnLinks.instance.takeInitialLink();
    accept(initial, allowCancel: false);
  }

  final linkSub = StripeReturnLinks.instance.stream.listen((uri) {
    accept(uri, allowCancel: true);
  });

  var leftApp = false;
  final lifecycle = AppLifecycleListener(
    onHide: () => leftApp = true,
    onPause: () => leftApp = true,
    onResume: () {
      if (!leftApp || completer.isCompleted) return;
      if (DateTime.now().difference(openedAt) < const Duration(seconds: 2)) {
        return;
      }
      Future<void>(() async {
        await Future<void>.delayed(const Duration(milliseconds: 400));
        if (completer.isCompleted || successArrived) {
          if (successArrived) finish(StripeCheckoutOutcome.success);
          return;
        }
        if (payments != null && paymentId != null) {
          final status = await fetchPaymentStatus(
            payments,
            paymentId,
            attempts: 2,
          );
          if (completer.isCompleted || successArrived) {
            if (successArrived) finish(StripeCheckoutOutcome.success);
            return;
          }
          if (status == PaymentStatus.completed) {
            finish(StripeCheckoutOutcome.success);
            return;
          }
        }
        if (!completer.isCompleted && !successArrived) {
          finish(StripeCheckoutOutcome.dismissed);
        }
      });
    },
  );

  try {
    if (completer.isCompleted) {
      final early = await completer.future;
      if (early == StripeCheckoutOutcome.success) {
        await PendingCheckoutStore.clear();
      }
      return early;
    }
    if (paymentId != null) {
      await PendingCheckoutStore.write(paymentId, stripeCheckoutSessionId);
    }

    var launched = false;
    try {
      launched = await launchUrl(
        checkoutUri,
        mode: LaunchMode.inAppBrowserView,
        browserConfiguration: const BrowserConfiguration(showTitle: true),
      );
    } catch (_) {}
    if (!launched) {
      try {
        launched = await launchUrl(
          checkoutUri,
          mode: LaunchMode.inAppWebView,
        );
      } catch (_) {}
    }
    if (!launched) {
      await PendingCheckoutStore.clear();
      return StripeCheckoutOutcome.failedToLaunch;
    }

    final outcome = await completer.future.timeout(
      const Duration(minutes: 20),
      onTimeout: () => StripeCheckoutOutcome.dismissed,
    );
    if (outcome == StripeCheckoutOutcome.success ||
        outcome == StripeCheckoutOutcome.cancelled ||
        outcome == StripeCheckoutOutcome.dismissed) {
      await PendingCheckoutStore.clear();
    }
    try {
      await closeInAppWebView();
    } catch (_) {}
    return outcome;
  } finally {
    await linkSub.cancel();
    lifecycle.dispose();
  }
}

/// After a cold start, a stored checkout is checked against the launch link
/// and the payment status endpoint before it is treated as dismissed.
Future<void> recoverInterruptedCheckout(PaymentProvider payments) async {
  await StripeReturnLinks.instance.ensureInitialized();
  final pending = await PendingCheckoutStore.read();
  if (pending == null) return;

  final initial = StripeReturnLinks.instance.peekInitialLink();
  final successLink = initial != null &&
      StripeReturnLinks.successMatches(initial, pending.sessionId);
  final status = await fetchPaymentStatus(
    payments,
    pending.paymentId,
    attempts: 2,
  );

  if (status == PaymentStatus.failed || status == PaymentStatus.refunded) {
    if (successLink) StripeReturnLinks.instance.takeInitialLink();
    await PendingCheckoutStore.clear();
    return;
  }

  final completed = status == PaymentStatus.completed || successLink;
  if (successLink) StripeReturnLinks.instance.takeInitialLink();
  await PendingCheckoutStore.clear();
  if (!completed) return;

  wildNestMessengerKey.currentState?.showSnackBar(
    const SnackBar(content: Text('Payment completed.')),
  );
}

Future<PaymentStatus?> fetchPaymentStatus(
  PaymentProvider payments,
  int? paymentId, {
  int attempts = 6,
}) async {
  if (paymentId == null) return null;
  PaymentStatus? last;
  for (var i = 0; i < attempts; i++) {
    try {
      final payment = await payments.getById(paymentId);
      last = payment.status;
      if (last == PaymentStatus.completed || last == PaymentStatus.failed) {
        return last;
      }
    } catch (_) {}
    if (i < attempts - 1) {
      await Future<void>.delayed(const Duration(milliseconds: 800));
    }
  }
  return last;
}

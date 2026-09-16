import 'dart:async';

import 'package:app_links/app_links.dart';
import 'package:ecommerce_mobile/models/payment.dart';
import 'package:ecommerce_mobile/providers/payment_provider.dart';
import 'package:flutter/widgets.dart';
import 'package:url_launcher/url_launcher.dart';

enum StripeCheckoutOutcome { success, cancelled, dismissed, failedToLaunch }

class StripeReturnLinks {
  static const scheme = 'wildnest';
  static const successHost = 'payment-success';
  static const cancelHost = 'payment-cancel';

  static final instance = StripeReturnLinks._();
  StripeReturnLinks._();

  final _controller = StreamController<Uri>.broadcast();
  StreamSubscription<Uri>? _sub;

  Stream<Uri> get stream => _controller.stream;

  void ensureInitialized() {
    if (_sub != null) return;
    _sub = AppLinks().uriLinkStream.listen((uri) {
      if (uri.scheme == scheme) {
        _controller.add(uri);
      }
    });
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
}

Future<StripeCheckoutOutcome> launchStripeCheckout(String url) async {
  StripeReturnLinks.instance.ensureInitialized();
  final checkoutUri = Uri.parse(url);
  final completer = Completer<StripeCheckoutOutcome>();
  final openedAt = DateTime.now();

  void finish(StripeCheckoutOutcome outcome) {
    if (!completer.isCompleted) {
      completer.complete(outcome);
    }
  }

  final linkSub = StripeReturnLinks.instance.stream.listen((uri) {
    final outcome = StripeReturnLinks.outcomeFor(uri);
    if (outcome != null) finish(outcome);
  });

  var leftApp = false;
  final lifecycle = AppLifecycleListener(
    onHide: () => leftApp = true,
    onPause: () => leftApp = true,
    onResume: () {
      if (!leftApp) return;
      if (DateTime.now().difference(openedAt) < const Duration(seconds: 2)) {
        return;
      }
      Future<void>.delayed(const Duration(milliseconds: 400), () {
        finish(StripeCheckoutOutcome.dismissed);
      });
    },
  );

  try {
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
    if (!launched) return StripeCheckoutOutcome.failedToLaunch;

    final outcome = await completer.future.timeout(
      const Duration(minutes: 20),
      onTimeout: () => StripeCheckoutOutcome.dismissed,
    );
    try {
      await closeInAppWebView();
    } catch (_) {}
    return outcome;
  } finally {
    await linkSub.cancel();
    lifecycle.dispose();
  }
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

import 'dart:typed_data';

import 'package:ecommerce_desktop/config/api_config.dart';
import 'package:ecommerce_desktop/providers/auth_provider.dart';
import 'package:ecommerce_desktop/theme/app_colors.dart';
import 'package:ecommerce_desktop/utils/formatters.dart';
import 'package:ecommerce_desktop/utils/image_bytes.dart';
import 'package:ecommerce_desktop/widgets/status_chip.dart';
import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;

class EntityThumbnail extends StatelessWidget {
  const EntityThumbnail({super.key, this.url, this.base64Image});

  final String? url;
  final String? base64Image;

  @override
  Widget build(BuildContext context) {
    final inline = decodeImageBytes(base64Image);
    final image = url?.trim();
    Widget child;
    if (inline != null) {
      child = Image.memory(inline, fit: BoxFit.cover, gaplessPlayback: true);
    } else if (image == null || image.isEmpty) {
      child = const ColoredBox(
        color: AppColors.border,
        child: Icon(Icons.image_outlined, size: 18, color: AppColors.textMuted),
      );
    } else if (image.startsWith('/')) {
      child = _AuthorizedImage(path: image);
    } else {
      child = Image.network(
        image,
        fit: BoxFit.cover,
        errorBuilder: (_, _, _) => const ColoredBox(
          color: AppColors.border,
          child: Icon(Icons.broken_image_outlined, size: 18, color: AppColors.textMuted),
        ),
      );
    }

    return ClipRRect(
      borderRadius: BorderRadius.circular(8),
      child: SizedBox(width: 42, height: 42, child: child),
    );
  }
}

class _AuthorizedImage extends StatefulWidget {
  const _AuthorizedImage({required this.path});

  final String path;

  @override
  State<_AuthorizedImage> createState() => _AuthorizedImageState();
}

class _AuthorizedImageState extends State<_AuthorizedImage> {
  Uint8List? _bytes;
  bool _failed = false;

  @override
  void initState() {
    super.initState();
    _load();
  }

  @override
  void didUpdateWidget(covariant _AuthorizedImage oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.path != widget.path) {
      _bytes = null;
      _failed = false;
      _load();
    }
  }

  Future<void> _load() async {
    try {
      final token = AuthProvider.accesstoken;
      final headers = <String, String>{};
      if (token != null && token.isNotEmpty) {
        headers['Authorization'] = 'Bearer $token';
      }
      final relative = widget.path.startsWith('/') ? widget.path.substring(1) : widget.path;
      final response = await http.get(Uri.parse('${ApiConfig.baseUrl}$relative'), headers: headers);
      if (!mounted) return;
      if (response.statusCode >= 300 || response.bodyBytes.isEmpty) {
        setState(() => _failed = true);
        return;
      }
      setState(() => _bytes = response.bodyBytes);
    } catch (_) {
      if (mounted) setState(() => _failed = true);
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_bytes != null) {
      return Image.memory(_bytes!, fit: BoxFit.cover, gaplessPlayback: true);
    }
    return ColoredBox(
      color: AppColors.border,
      child: Icon(
        _failed ? Icons.broken_image_outlined : Icons.image_outlined,
        size: 18,
        color: AppColors.textMuted,
      ),
    );
  }
}

class IconActionButton extends StatelessWidget {
  const IconActionButton({
    super.key,
    required this.icon,
    required this.color,
    required this.tooltip,
    required this.enabled,
    required this.onPressed,
  });

  final IconData icon;
  final Color color;
  final String tooltip;
  final bool enabled;
  final VoidCallback onPressed;

  @override
  Widget build(BuildContext context) {
    return Tooltip(
      message: tooltip,
      child: InkWell(
        onTap: enabled ? onPressed : () {},
        borderRadius: BorderRadius.circular(8),
        child: Container(
          width: 34,
          height: 34,
          decoration: BoxDecoration(
            borderRadius: BorderRadius.circular(8),
            border: Border.all(color: AppColors.border),
          ),
          child: Icon(icon, size: 18, color: enabled ? color : color.withValues(alpha: 0.35)),
        ),
      ),
    );
  }
}

class ListError extends StatelessWidget {
  const ListError({super.key, required this.message, required this.onRetry});

  final String message;
  final VoidCallback onRetry;

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Text(message, textAlign: TextAlign.center),
          const SizedBox(height: 12),
          OutlinedButton(onPressed: onRetry, child: const Text('Try again')),
        ],
      ),
    );
  }
}

class ListPager extends StatelessWidget {
  const ListPager({
    super.key,
    required this.page,
    required this.pageSize,
    required this.total,
    required this.onPage,
  });

  final int page;
  final int pageSize;
  final int total;
  final ValueChanged<int> onPage;

  @override
  Widget build(BuildContext context) {
    final from = total == 0 ? 0 : ((page - 1) * pageSize) + 1;
    final to = (page * pageSize) > total ? total : page * pageSize;
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 4),
      child: Row(
        children: [
          Text(
            '$from–$to of $total',
            style: const TextStyle(color: AppColors.textMuted, fontSize: 13),
          ),
          const Spacer(),
          IconButton(
            tooltip: 'Previous page',
            onPressed: page > 1 ? () => onPage(page - 1) : null,
            icon: const Icon(Icons.chevron_left),
          ),
          IconButton(
            tooltip: 'Next page',
            onPressed: page * pageSize < total ? () => onPage(page + 1) : null,
            icon: const Icon(Icons.chevron_right),
          ),
        ],
      ),
    );
  }
}

class DateFilterField extends StatelessWidget {
  const DateFilterField({
    super.key,
    required this.label,
    required this.value,
    required this.onPick,
    required this.onClear,
  });

  final String label;
  final DateTime? value;
  final VoidCallback onPick;
  final VoidCallback? onClear;

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      width: 210,
      child: InputDecorator(
        decoration: InputDecoration(
          labelText: label,
          isDense: true,
          suffixIcon: onClear == null
              ? const Icon(Icons.calendar_today_outlined, size: 18)
              : IconButton(
                  tooltip: 'Clear',
                  onPressed: onClear,
                  icon: const Icon(Icons.close, size: 18),
                ),
        ),
        child: InkWell(
          onTap: onPick,
          child: Text(
            value == null ? 'Select' : formatDate(value!),
            overflow: TextOverflow.ellipsis,
          ),
        ),
      ),
    );
  }
}

class EllipsisText extends StatelessWidget {
  const EllipsisText(
    this.text, {
    super.key,
    this.style,
    this.maxLines = 1,
  });

  final String text;
  final TextStyle? style;
  final int maxLines;

  @override
  Widget build(BuildContext context) {
    return Tooltip(
      message: text,
      child: Text(
        text,
        maxLines: maxLines,
        overflow: TextOverflow.ellipsis,
        style: style,
      ),
    );
  }
}

Widget labeledStatusChip({required String label, required String tone}) {
  switch (tone) {
    case 'success':
      return StatusChip(active: true, label: label);
    case 'warning':
      return StatusChip(
        active: false,
        label: label,
        background: AppColors.chipInactive,
        foreground: AppColors.chipInactiveText,
      );
    case 'danger':
      return StatusChip(
        active: false,
        label: label,
        background: AppColors.errorBg,
        foreground: AppColors.errorText,
      );
    default:
      return StatusChip(
        active: false,
        label: label,
        background: const Color(0xFFE8E6E1),
        foreground: AppColors.textMuted,
      );
  }
}

Future<bool> confirmAdminAction({
  required BuildContext context,
  required String title,
  required String message,
  required String confirmLabel,
  bool destructive = false,
}) async {
  final confirmed = await showDialog<bool>(
    context: context,
    builder: (context) => AlertDialog(
      title: Text(title),
      content: Text(message),
      actions: [
        TextButton(onPressed: () => Navigator.pop(context, false), child: const Text('Cancel')),
        FilledButton(
          style: destructive ? FilledButton.styleFrom(backgroundColor: AppColors.danger) : null,
          onPressed: () => Navigator.pop(context, true),
          child: Text(confirmLabel),
        ),
      ],
    ),
  );
  return confirmed == true;
}

Future<String?> askForReason({
  required BuildContext context,
  required String title,
  required String message,
  required String confirmLabel,
}) {
  return showDialog<String>(
    context: context,
    builder: (context) => _ReasonDialog(title: title, message: message, confirmLabel: confirmLabel),
  );
}

class _ReasonDialog extends StatefulWidget {
  const _ReasonDialog({
    required this.title,
    required this.message,
    required this.confirmLabel,
  });

  final String title;
  final String message;
  final String confirmLabel;

  @override
  State<_ReasonDialog> createState() => _ReasonDialogState();
}

class _ReasonDialogState extends State<_ReasonDialog> {
  final _formKey = GlobalKey<FormState>();
  final _reason = TextEditingController();

  @override
  void dispose() {
    _reason.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return AlertDialog(
      title: Text(widget.title),
      content: SizedBox(
        width: 420,
        child: Form(
          key: _formKey,
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Text(widget.message),
              const SizedBox(height: 12),
              TextFormField(
                controller: _reason,
                maxLength: 500,
                minLines: 2,
                maxLines: 4,
                autovalidateMode: AutovalidateMode.onUserInteraction,
                decoration: const InputDecoration(
                  labelText: 'Reason',
                  alignLabelWithHint: true,
                  errorMaxLines: 2,
                ),
                validator: (value) {
                  final text = value?.trim() ?? '';
                  if (text.isEmpty) return 'A reason is required.';
                  if (text.length > 500) return 'Reason must be at most 500 characters.';
                  return null;
                },
              ),
            ],
          ),
        ),
      ),
      actions: [
        TextButton(onPressed: () => Navigator.pop(context), child: const Text('Cancel')),
        FilledButton(
          style: FilledButton.styleFrom(backgroundColor: AppColors.danger),
          onPressed: () {
            if (_formKey.currentState?.validate() != true) return;
            Navigator.pop(context, _reason.text.trim());
          },
          child: Text(widget.confirmLabel),
        ),
      ],
    );
  }
}

class DetailLine extends StatelessWidget {
  const DetailLine({super.key, required this.label, required this.value});

  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 10),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SizedBox(
            width: 148,
            child: Text(label, style: const TextStyle(color: AppColors.textMuted)),
          ),
          Expanded(child: SelectableText(value)),
        ],
      ),
    );
  }
}

class AdminScreenFrame extends StatelessWidget {
  const AdminScreenFrame({
    super.key,
    required this.title,
    required this.subtitle,
    required this.filters,
    required this.body,
  });

  final String title;
  final String subtitle;
  final Widget filters;
  final Widget body;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.all(28),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            title,
            style: const TextStyle(fontSize: 26, fontWeight: FontWeight.w700, color: AppColors.text),
          ),
          const SizedBox(height: 4),
          Text(subtitle, style: const TextStyle(color: AppColors.textMuted)),
          const SizedBox(height: 18),
          filters,
          const SizedBox(height: 16),
          Expanded(child: body),
        ],
      ),
    );
  }
}

class AdminTableCard extends StatelessWidget {
  const AdminTableCard({
    super.key,
    required this.header,
    required this.empty,
    required this.rows,
    required this.pager,
    this.minWidth = 1080,
  });

  final Widget header;
  final String empty;
  final List<Widget> rows;
  final Widget pager;
  final double minWidth;

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: BoxDecoration(
        color: AppColors.surface,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: AppColors.border),
      ),
      child: Column(
        children: [
          Expanded(
            child: LayoutBuilder(
              builder: (context, constraints) {
                final width = constraints.maxWidth < minWidth ? minWidth : constraints.maxWidth;
                return SingleChildScrollView(
                  scrollDirection: Axis.horizontal,
                  child: SizedBox(
                    width: width,
                    height: constraints.maxHeight,
                    child: Column(
                      children: [
                        header,
                        const Divider(height: 1),
                        Expanded(
                          child: rows.isEmpty
                              ? Center(
                                  child: Text(empty, style: const TextStyle(color: AppColors.textMuted)),
                                )
                              : ListView.separated(
                                  itemCount: rows.length,
                                  separatorBuilder: (_, _) => const Divider(height: 1),
                                  itemBuilder: (context, index) => rows[index],
                                ),
                        ),
                      ],
                    ),
                  ),
                );
              },
            ),
          ),
          const Divider(height: 1),
          pager,
        ],
      ),
    );
  }
}

String friendlyError(Object error) {
  return error.toString().replaceFirst('Bad state: ', '');
}

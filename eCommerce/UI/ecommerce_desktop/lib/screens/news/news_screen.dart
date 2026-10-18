import 'dart:async';
import 'dart:typed_data';

import 'package:ecommerce_desktop/models/news_item.dart';
import 'package:ecommerce_desktop/providers/news_provider.dart';
import 'package:ecommerce_desktop/theme/app_colors.dart';
import 'package:ecommerce_desktop/utils/api_client_exception.dart';
import 'package:ecommerce_desktop/utils/form_rules.dart';
import 'package:ecommerce_desktop/utils/formatters.dart';
import 'package:ecommerce_desktop/utils/image_bytes.dart';
import 'package:ecommerce_desktop/widgets/admin_list.dart';
import 'package:ecommerce_desktop/widgets/image_field.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

class NewsScreen extends StatefulWidget {
  const NewsScreen({super.key});

  @override
  State<NewsScreen> createState() => _NewsScreenState();
}

class _NewsScreenState extends State<NewsScreen> {
  static const _pageSize = 10;

  final _searchController = TextEditingController();
  Timer? _debounce;
  int _requestId = 0;
  int _page = 1;
  int _total = 0;
  String? _status;
  bool _loading = true;
  String? _error;
  List<NewsItem> _items = [];

  @override
  void initState() {
    super.initState();
    _load();
  }

  @override
  void dispose() {
    _debounce?.cancel();
    _searchController.dispose();
    super.dispose();
  }

  Future<void> _load() async {
    final requestId = ++_requestId;
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final result = await context.read<NewsProvider>().get(
        filter: {
          'FTS': _searchController.text.trim(),
          'IsPublished': _status == null ? null : _status == 'published',
          'Page': _page,
          'PageSize': _pageSize,
          'IncludeTotalCount': true,
        },
      );
      if (!mounted || requestId != _requestId) return;
      final items = result.items ?? [];
      final total = result.totalCount ?? items.length;
      if (items.isEmpty && total > 0 && _page > 1) {
        setState(() => _page -= 1);
        await _load();
        return;
      }
      setState(() {
        _items = items;
        _total = total;
        _loading = false;
      });
    } on SessionExpiredException {
      return;
    } catch (error) {
      if (!mounted || requestId != _requestId) return;
      setState(() {
        _error = friendlyError(error);
        _loading = false;
      });
    }
  }

  void _apply(VoidCallback change) {
    setState(() {
      change();
      _page = 1;
    });
    _load();
  }

  Future<void> _edit({NewsItem? item}) async {
    final saved = await showDialog<NewsItem>(
      context: context,
      builder: (_) => _NewsDialog(item: item),
    );
    if (saved == null || !mounted) return;
    _toast(item == null ? 'News item added.' : 'News item updated.');
    if (item == null) _page = 1;
    await _load();
  }

  Future<void> _delete(NewsItem item) async {
    final confirmed = await confirmAdminAction(
      context: context,
      title: 'Delete news',
      message: 'Delete ${item.title}?',
      confirmLabel: 'Delete',
      destructive: true,
    );
    if (!confirmed || !mounted) return;
    try {
      await context.read<NewsProvider>().remove(item.id);
      if (!mounted) return;
      _toast('News item deleted.');
      await _load();
    } on SessionExpiredException {
      return;
    } catch (error) {
      if (!mounted) return;
      _toast(friendlyError(error));
    }
  }

  Future<void> _details(NewsItem item) async {
    await showDialog<void>(
      context: context,
      builder: (context) => AlertDialog(
        title: Text(item.title),
        content: SizedBox(
          width: 520,
          child: SingleChildScrollView(
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Align(alignment: Alignment.centerLeft, child: EntityThumbnail(url: item.imageUrl)),
                const SizedBox(height: 12),
                DetailLine(label: 'Date and time', value: formatDateTime(item.publishedAt)),
                DetailLine(label: 'Author', value: _text(item.authorName)),
                DetailLine(label: 'Status', value: item.isPublished ? 'Published' : 'Unpublished'),
                const SizedBox(height: 8),
                Text(item.text, style: const TextStyle(height: 1.4)),
              ],
            ),
          ),
        ),
        actions: [
          TextButton(onPressed: () => Navigator.pop(context), child: const Text('Close')),
        ],
      ),
    );
  }

  void _toast(String message) {
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(message)));
  }

  @override
  Widget build(BuildContext context) {
    return AdminScreenFrame(
      title: 'News',
      subtitle: 'Announcements for visitors. Unpublished items stay hidden in the mobile app.',
      filters: Wrap(
        spacing: 12,
        runSpacing: 12,
        crossAxisAlignment: WrapCrossAlignment.center,
        children: [
          SizedBox(
            width: 320,
            child: TextField(
              controller: _searchController,
              onChanged: (_) {
                _debounce?.cancel();
                _debounce = Timer(const Duration(milliseconds: 300), () {
                  if (!mounted) return;
                  setState(() => _page = 1);
                  _load();
                });
              },
              decoration: const InputDecoration(
                hintText: 'Search by title or text...',
                prefixIcon: Icon(Icons.search),
                isDense: true,
              ),
            ),
          ),
          SizedBox(width: 200, child: _statusFilter()),
          FilledButton.icon(
            onPressed: () => _edit(),
            icon: const Icon(Icons.add, size: 18),
            label: const Text('Add news'),
          ),
        ],
      ),
      body: _body(),
    );
  }

  Widget _body() {
    if (_loading) return const Center(child: CircularProgressIndicator());
    if (_error != null) return ListError(message: _error!, onRetry: _load);
    const headerStyle = TextStyle(fontSize: 12, fontWeight: FontWeight.w700, color: AppColors.textMuted);
    return AdminTableCard(
      empty: 'No news to show.',
      pager: ListPager(
        page: _page,
        pageSize: _pageSize,
        total: _total,
        onPage: (page) {
          setState(() => _page = page);
          _load();
        },
      ),
      header: const Padding(
        padding: EdgeInsets.symmetric(horizontal: 16, vertical: 12),
        child: Row(
          children: [
            SizedBox(width: 54),
            Expanded(flex: 4, child: Text('Title', style: headerStyle)),
            Expanded(flex: 3, child: Text('Date and time', style: headerStyle)),
            Expanded(flex: 2, child: Text('Author', style: headerStyle)),
            Expanded(flex: 2, child: Text('Status', style: headerStyle)),
            SizedBox(width: 136, child: Text('Actions', style: headerStyle)),
          ],
        ),
      ),
      rows: [
        for (final item in _items)
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
            child: Row(
              children: [
                EntityThumbnail(url: item.imageUrl),
                const SizedBox(width: 12),
                Expanded(
                  flex: 4,
                  child: EllipsisText(item.title, style: const TextStyle(fontWeight: FontWeight.w600)),
                ),
                Expanded(flex: 3, child: EllipsisText(formatDateTime(item.publishedAt))),
                Expanded(flex: 2, child: EllipsisText(_text(item.authorName))),
                Expanded(
                  flex: 2,
                  child: Align(
                    alignment: Alignment.centerLeft,
                    child: labeledStatusChip(
                      label: item.isPublished ? 'Published' : 'Unpublished',
                      tone: item.isPublished ? 'success' : 'neutral',
                    ),
                  ),
                ),
                SizedBox(
                  width: 136,
                  child: Row(
                    children: [
                      IconActionButton(
                        icon: Icons.visibility_outlined,
                        color: AppColors.text,
                        tooltip: 'View details',
                        enabled: true,
                        onPressed: () => _details(item),
                      ),
                      IconActionButton(
                        icon: Icons.edit_outlined,
                        color: AppColors.text,
                        tooltip: 'Edit',
                        enabled: true,
                        onPressed: () => _edit(item: item),
                      ),
                      IconActionButton(
                        icon: Icons.delete_outline,
                        color: AppColors.danger,
                        tooltip: 'Delete',
                        enabled: true,
                        onPressed: () => _delete(item),
                      ),
                    ],
                  ),
                ),
              ],
            ),
          ),
      ],
    );
  }

  Widget _statusFilter() {
    return DropdownButtonFormField<String?>(
      key: ValueKey('news-status-$_status'),
      initialValue: _status,
      isExpanded: true,
      decoration: const InputDecoration(labelText: 'Status', isDense: true),
      items: const [
        DropdownMenuItem(value: null, child: Text('All statuses', overflow: TextOverflow.ellipsis)),
        DropdownMenuItem(value: 'published', child: Text('Published', overflow: TextOverflow.ellipsis)),
        DropdownMenuItem(value: 'unpublished', child: Text('Unpublished', overflow: TextOverflow.ellipsis)),
      ],
      onChanged: (value) => _apply(() => _status = value),
    );
  }
}

class _NewsDialog extends StatefulWidget {
  const _NewsDialog({this.item});

  final NewsItem? item;

  @override
  State<_NewsDialog> createState() => _NewsDialogState();
}

class _NewsDialogState extends State<_NewsDialog> {
  final _formKey = GlobalKey<FormState>();
  late final TextEditingController _title;
  late final TextEditingController _text;
  late final TextEditingController _when;
  late bool _isPublished;
  DateTime? _publishedAt;
  Uint8List? _pickedBytes;
  String? _pickedBase64;
  String? _pickedType;
  String? _existingUrl;
  bool _saving = false;
  String? _formError;
  String? _imageError;

  bool get _isEdit => widget.item != null;

  @override
  void initState() {
    super.initState();
    final item = widget.item;
    _title = TextEditingController(text: item?.title ?? '');
    _text = TextEditingController(text: item?.text ?? '');
    _publishedAt = item?.publishedAt.toLocal();
    _when = TextEditingController(text: _publishedAt == null ? '' : formatDateTime(_publishedAt!));
    _isPublished = item?.isPublished ?? true;
    _existingUrl = item?.imageUrl;
  }

  @override
  void dispose() {
    _title.dispose();
    _text.dispose();
    _when.dispose();
    super.dispose();
  }

  Future<void> _pickImage() async {
    final result = await pickPngOrJpeg();
    if (!mounted) return;
    setState(() {
      _imageError = result.error;
      if (result.image != null) {
        _pickedBytes = result.image!.bytes;
        _pickedBase64 = result.image!.base64;
        _pickedType = result.image!.contentType;
      }
    });
  }

  Future<void> _pickWhen() async {
    final current = _publishedAt ?? DateTime.now();
    final date = await showDatePicker(
      context: context,
      initialDate: current,
      firstDate: DateTime(2000),
      lastDate: DateTime(2100, 12, 31),
      helpText: 'News date',
    );
    if (date == null || !mounted) return;
    final time = await showTimePicker(
      context: context,
      initialTime: TimeOfDay.fromDateTime(current),
      helpText: 'News time',
    );
    if (time == null || !mounted) return;
    final chosen = DateTime(date.year, date.month, date.day, time.hour, time.minute);
    setState(() {
      _publishedAt = chosen;
      _when.text = formatDateTime(chosen);
    });
  }

  Future<void> _save() async {
    final imageMessage = _pickedBytes == null
        ? (_isEdit && _existingUrl != null ? null : 'Image is required. Use a PNG or JPEG file of at most 512 KB.')
        : imageValidationMessage(_pickedBytes!);
    setState(() => _imageError = imageMessage);
    if (_formKey.currentState?.validate() != true || imageMessage != null) return;
    setState(() {
      _saving = true;
      _formError = null;
    });
    final body = <String, dynamic>{
      'title': _title.text.trim(),
      'text': _text.text.trim(),
      'publishedAt': _publishedAt!.toUtc().toIso8601String(),
      'isPublished': _isPublished,
      'imageBase64': _pickedBase64,
      'imageContentType': _pickedType,
    };
    try {
      final provider = context.read<NewsProvider>();
      final saved = _isEdit ? await provider.update(widget.item!.id, body) : await provider.insert(body);
      if (!mounted) return;
      Navigator.pop(context, saved);
    } on SessionExpiredException {
      return;
    } catch (error) {
      if (!mounted) return;
      setState(() {
        _saving = false;
        _formError = friendlyError(error);
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    return Dialog(
      child: ConstrainedBox(
        constraints: const BoxConstraints(maxWidth: 560, maxHeight: 760),
        child: Padding(
          padding: const EdgeInsets.fromLTRB(24, 16, 16, 18),
          child: Form(
            key: _formKey,
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Row(
                  children: [
                    Expanded(
                      child: Text(
                        _isEdit ? 'Edit news' : 'Add news',
                        style: const TextStyle(fontSize: 20, fontWeight: FontWeight.w700),
                      ),
                    ),
                    IconButton(
                      tooltip: 'Close',
                      onPressed: _saving ? null : () => Navigator.pop(context),
                      icon: const Icon(Icons.close),
                    ),
                  ],
                ),
                Flexible(
                  child: SingleChildScrollView(
                    padding: const EdgeInsets.only(right: 8),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.stretch,
                      children: [
                        ImageField(
                          bytes: _pickedBytes,
                          existingUrl: _pickedBytes == null ? _existingUrl : null,
                          error: _imageError,
                          onPick: _pickImage,
                          onClear: _pickedBytes == null
                              ? null
                              : () => setState(() {
                                  _pickedBytes = null;
                                  _pickedBase64 = null;
                                  _pickedType = null;
                                  _imageError = null;
                                }),
                        ),
                        if (_isEdit && _pickedBytes == null)
                          const Padding(
                            padding: EdgeInsets.only(top: 6),
                            child: Text(
                              'The current image is kept until you upload a new one.',
                              style: TextStyle(color: AppColors.textMuted, fontSize: 12),
                            ),
                          ),
                        const SizedBox(height: 12),
                        TextFormField(
                          controller: _title,
                          autovalidateMode: AutovalidateMode.onUserInteraction,
                          decoration: const InputDecoration(labelText: 'Title', isDense: true, errorMaxLines: 2),
                          validator: (value) => requiredText(value, 'Title', max: 150),
                        ),
                        const SizedBox(height: 12),
                        TextFormField(
                          controller: _text,
                          maxLines: 6,
                          maxLength: 4000,
                          autovalidateMode: AutovalidateMode.onUserInteraction,
                          decoration: const InputDecoration(
                            labelText: 'Text',
                            alignLabelWithHint: true,
                            errorMaxLines: 2,
                          ),
                          validator: (value) => requiredText(value, 'Text', max: 4000),
                        ),
                        const SizedBox(height: 12),
                        TextFormField(
                          controller: _when,
                          readOnly: true,
                          onTap: _saving ? null : _pickWhen,
                          autovalidateMode: AutovalidateMode.onUserInteraction,
                          decoration: const InputDecoration(
                            labelText: 'Date and time',
                            isDense: true,
                            helperText: 'Choose a date, then a time. Shown to visitors in local time.',
                            helperMaxLines: 2,
                            errorMaxLines: 2,
                            suffixIcon: Icon(Icons.calendar_month_outlined),
                          ),
                          validator: (_) => _publishedAt == null ? 'Date and time are required.' : null,
                        ),
                        SwitchListTile(
                          contentPadding: EdgeInsets.zero,
                          title: const Text('Published'),
                          subtitle: const Text('Unpublished news stays hidden in the mobile app.'),
                          value: _isPublished,
                          onChanged: _saving ? null : (value) => setState(() => _isPublished = value),
                        ),
                        if (_formError != null) Text(_formError!, style: const TextStyle(color: AppColors.danger)),
                      ],
                    ),
                  ),
                ),
                const SizedBox(height: 12),
                Row(
                  mainAxisAlignment: MainAxisAlignment.end,
                  children: [
                    TextButton(onPressed: _saving ? null : () => Navigator.pop(context), child: const Text('Cancel')),
                    const SizedBox(width: 8),
                    FilledButton(onPressed: _saving ? null : _save, child: Text(_saving ? 'Saving...' : 'Save')),
                  ],
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

String _text(String? value) {
  final text = value?.trim() ?? '';
  return text.isEmpty ? '—' : text;
}

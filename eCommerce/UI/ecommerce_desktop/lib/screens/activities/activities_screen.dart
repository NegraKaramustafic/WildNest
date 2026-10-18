import 'dart:async';

import 'package:ecommerce_desktop/models/activity.dart';
import 'package:ecommerce_desktop/models/activity_type.dart';
import 'package:ecommerce_desktop/providers/activity_provider.dart';
import 'package:ecommerce_desktop/providers/activity_type_provider.dart';
import 'package:ecommerce_desktop/screens/activities/activity_form_dialog.dart';
import 'package:ecommerce_desktop/theme/app_colors.dart';
import 'package:ecommerce_desktop/utils/api_client_exception.dart';
import 'package:ecommerce_desktop/utils/formatters.dart';
import 'package:ecommerce_desktop/widgets/status_chip.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

class ActivitiesScreen extends StatefulWidget {
  const ActivitiesScreen({super.key});

  @override
  State<ActivitiesScreen> createState() => _ActivitiesScreenState();
}

class _ActivitiesScreenState extends State<ActivitiesScreen> {
  static const _pageSize = 10;

  final _searchController = TextEditingController();
  Timer? _debounce;
  int _requestId = 0;
  int _page = 1;
  int _total = 0;

  List<ActivityType> _activityTypes = [];
  int? _activityTypeId;
  ActivityStatus? _status;

  bool _loading = true;
  String? _error;
  List<Activity> _activities = [];

  @override
  void initState() {
    super.initState();
    _loadTypes();
    _load();
  }

  Future<void> _loadTypes() async {
    try {
      final types = await context.read<ActivityTypeProvider>().getAll();
      if (!mounted) return;
      setState(() {
        _activityTypes = types;
        if (_activityTypeId != null && types.every((type) => type.id != _activityTypeId)) {
          _activityTypeId = null;
        }
      });
    } on SessionExpiredException {
      return;
    } catch (_) {}
  }

  Future<List<ActivityType>?> _requireTypes() async {
    try {
      final types = await context.read<ActivityTypeProvider>().getAll();
      if (!mounted) return null;
      setState(() => _activityTypes = types);
      if (types.isEmpty) {
        _showError('There are no activity types. Add an activity type before adding an activity.');
        return null;
      }
      return types;
    } on SessionExpiredException {
      return null;
    } catch (e) {
      if (mounted) _showError(_message(e));
      return null;
    }
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
      final provider = context.read<ActivityProvider>();
      final result = await provider.get(
        filter: {
          'Name': _searchController.text.trim(),
          'ActivityTypeId': _activityTypeId,
          'Status': _status == null ? null : activityStatusApiValue(_status!),
          'Page': _page,
          'PageSize': _pageSize,
          'IncludeTotalCount': true,
        },
      );
      if (!mounted || requestId != _requestId) return;
      setState(() {
        _activities = result.items ?? [];
        _total = result.totalCount ?? _activities.length;
        _loading = false;
      });
    } on SessionExpiredException {
      return;
    } catch (e) {
      if (!mounted || requestId != _requestId) return;
      setState(() {
        _error = _message(e);
        _loading = false;
      });
    }
  }

  void _onSearchChanged(String value) {
    _debounce?.cancel();
    _debounce = Timer(const Duration(milliseconds: 300), () {
      if (!mounted) return;
      setState(() => _page = 1);
      _load();
    });
  }

  void _changePage(int page) {
    setState(() => _page = page);
    _load();
  }

  Future<void> _add() async {
    final types = await _requireTypes();
    if (types == null || !mounted) return;
    final result = await showActivityFormDialog(context, types: types);
    if (result == null || !mounted) return;
    if (result.created != null) {
      await _offerActivate(result.created!);
    }
    if (mounted) await _load();
  }

  Future<void> _offerActivate(Activity activity) async {
    final activate = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Activate now?'),
        content: Text(
          '“${activity.name}” was saved as a draft. Do you want to activate it now?',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: const Text('Not now'),
          ),
          FilledButton(
            onPressed: () => Navigator.pop(context, true),
            child: const Text('Activate now'),
          ),
        ],
      ),
    );
    if (activate != true || !mounted) return;

    try {
      final provider = context.read<ActivityProvider>();
      await provider.activate(activity.id);
    } on SessionExpiredException {
      return;
    } catch (e) {
      if (!mounted) return;
      _showError(_message(e));
    }
  }

  Future<void> _edit(Activity activity) async {
    final types = await _requireTypes();
    if (types == null || !mounted) return;
    final result = await showActivityFormDialog(context, activity: activity, types: types);
    if (result != null && mounted) await _load();
  }

  Future<void> _activate(Activity activity) async {
    try {
      final provider = context.read<ActivityProvider>();
      await provider.activate(activity.id);
      if (mounted) await _load();
    } on SessionExpiredException {
      return;
    } catch (e) {
      if (!mounted) return;
      _showError(_message(e));
    }
  }

  Future<void> _deactivate(Activity activity) async {
    try {
      final provider = context.read<ActivityProvider>();
      await provider.deactivate(activity.id);
      if (mounted) await _load();
    } on SessionExpiredException {
      return;
    } catch (e) {
      if (!mounted) return;
      _showError(_message(e));
    }
  }

  Future<void> _delete(Activity activity) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Delete activity'),
        content: Text('Delete ${activity.name}?'),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: const Text('Cancel'),
          ),
          FilledButton(
            style: FilledButton.styleFrom(backgroundColor: AppColors.danger),
            onPressed: () => Navigator.pop(context, true),
            child: const Text('Delete'),
          ),
        ],
      ),
    );
    if (confirmed != true || !mounted) return;

    try {
      final provider = context.read<ActivityProvider>();
      await provider.remove(activity.id);
      if (mounted) await _load();
    } on SessionExpiredException {
      return;
    } catch (e) {
      if (!mounted) return;
      _showError(_message(e));
    }
  }

  void _showError(String message) {
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(message)));
  }

  String _message(Object error) {
    final text = error.toString();
    return text.replaceFirst('Bad state: ', '');
  }

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.all(28),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      'Activities',
                      style: TextStyle(
                        fontSize: 26,
                        fontWeight: FontWeight.w700,
                        color: AppColors.text,
                      ),
                    ),
                    SizedBox(height: 4),
                    Text(
                      'View and edit activities',
                      style: TextStyle(color: AppColors.textMuted),
                    ),
                  ],
                ),
              ),
              FilledButton.icon(
                onPressed: _add,
                icon: const Icon(Icons.add, size: 18),
                label: const Text('Add activity'),
              ),
            ],
          ),
          const SizedBox(height: 18),
          Wrap(
            spacing: 12,
            runSpacing: 12,
            crossAxisAlignment: WrapCrossAlignment.center,
            children: [
              SizedBox(
                width: 280,
                child: TextField(
                  controller: _searchController,
                  onChanged: _onSearchChanged,
                  decoration: const InputDecoration(
                    hintText: 'Search by name...',
                    prefixIcon: Icon(Icons.search),
                    isDense: true,
                  ),
                ),
              ),
              SizedBox(width: 200, child: _typeFilter()),
              SizedBox(width: 200, child: _statusFilter()),
            ],
          ),
          const SizedBox(height: 16),
          Expanded(child: _buildBody()),
        ],
      ),
    );
  }

  Widget _typeFilter() {
    final selected = _activityTypes.any((type) => type.id == _activityTypeId)
        ? _activityTypeId
        : null;
    return DropdownButtonFormField<int?>(
      key: ValueKey('type-$selected-${_activityTypes.length}'),
      initialValue: selected,
      isExpanded: true,
      decoration: const InputDecoration(labelText: 'Type', isDense: true),
      items: [
        const DropdownMenuItem(value: null, child: Text('All types')),
        for (final type in _activityTypes)
          DropdownMenuItem(value: type.id, child: Text(type.name)),
      ],
      onChanged: (value) {
        setState(() {
          _activityTypeId = value;
          _page = 1;
        });
        _load();
      },
    );
  }

  Widget _statusFilter() {
    return DropdownButtonFormField<ActivityStatus?>(
      key: ValueKey('status-$_status'),
      initialValue: _status,
      isExpanded: true,
      decoration: const InputDecoration(labelText: 'Status', isDense: true),
      items: [
        const DropdownMenuItem(value: null, child: Text('All statuses')),
        for (final status in ActivityStatus.values)
          DropdownMenuItem(value: status, child: Text(activityStatusLabel(status))),
      ],
      onChanged: (value) {
        setState(() {
          _status = value;
          _page = 1;
        });
        _load();
      },
    );
  }

  Widget _buildBody() {
    if (_loading) {
      return const Center(child: CircularProgressIndicator());
    }
    if (_error != null) {
      return _ErrorState(message: _error!, onRetry: _load);
    }
    return _ActivitiesTable(
      activities: _activities,
      page: _page,
      pageSize: _pageSize,
      total: _total,
      onPage: _changePage,
      onEdit: _edit,
      onActivate: _activate,
      onDeactivate: _deactivate,
      onDelete: _delete,
    );
  }
}

class _ActivitiesTable extends StatelessWidget {
  const _ActivitiesTable({
    required this.activities,
    required this.page,
    required this.pageSize,
    required this.total,
    required this.onPage,
    required this.onEdit,
    required this.onActivate,
    required this.onDeactivate,
    required this.onDelete,
  });

  final List<Activity> activities;
  final int page;
  final int pageSize;
  final int total;
  final ValueChanged<int> onPage;
  final ValueChanged<Activity> onEdit;
  final ValueChanged<Activity> onActivate;
  final ValueChanged<Activity> onDeactivate;
  final ValueChanged<Activity> onDelete;

  @override
  Widget build(BuildContext context) {
    final from = total == 0 ? 0 : ((page - 1) * pageSize) + 1;
    final to = (page * pageSize) > total ? total : page * pageSize;
    final hasPrev = page > 1;
    final hasNext = page * pageSize < total;

    return Container(
      decoration: BoxDecoration(
        color: AppColors.surface,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: AppColors.border),
      ),
      child: Column(
        children: [
          const _HeaderRow(),
          const Divider(height: 1),
          Expanded(
            child: activities.isEmpty
                ? const Center(
                    child: Text(
                      'No activities to show.',
                      style: TextStyle(color: AppColors.textMuted),
                    ),
                  )
                : ListView.separated(
                    itemCount: activities.length,
                    separatorBuilder: (_, _) => const Divider(height: 1),
                    itemBuilder: (context, index) {
                      final activity = activities[index];
                      return _ActivityRow(
                        activity: activity,
                        onEdit: () => onEdit(activity),
                        onActivate: () => onActivate(activity),
                        onDeactivate: () => onDeactivate(activity),
                        onDelete: () => onDelete(activity),
                      );
                    },
                  ),
          ),
          const Divider(height: 1),
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 4),
            child: Row(
              children: [
                Text(
                  total == 0 ? '0 of 0' : '$from–$to of $total',
                  style: const TextStyle(color: AppColors.textMuted, fontSize: 13),
                ),
                const Spacer(),
                IconButton(
                  tooltip: 'Previous',
                  onPressed: hasPrev ? () => onPage(page - 1) : null,
                  icon: const Icon(Icons.chevron_left),
                ),
                Text('$page'),
                IconButton(
                  tooltip: 'Next',
                  onPressed: hasNext ? () => onPage(page + 1) : null,
                  icon: const Icon(Icons.chevron_right),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _HeaderRow extends StatelessWidget {
  const _HeaderRow();

  @override
  Widget build(BuildContext context) {
    return const Padding(
      padding: EdgeInsets.symmetric(horizontal: 18, vertical: 14),
      child: Row(
        children: [
          Expanded(flex: 4, child: _HeaderLabel('Activity')),
          Expanded(flex: 2, child: _HeaderLabel('Type')),
          Expanded(flex: 2, child: _HeaderLabel('Animal')),
          Expanded(flex: 2, child: _HeaderLabel('Duration')),
          Expanded(flex: 2, child: _HeaderLabel('Price')),
          Expanded(flex: 2, child: _HeaderLabel('Capacity')),
          Expanded(flex: 2, child: _HeaderLabel('Status')),
          SizedBox(width: 156, child: _HeaderLabel('Actions')),
        ],
      ),
    );
  }
}

class _HeaderLabel extends StatelessWidget {
  const _HeaderLabel(this.text);

  final String text;

  @override
  Widget build(BuildContext context) {
    return Text(
      text.toUpperCase(),
      style: const TextStyle(
        fontSize: 11,
        letterSpacing: 0.6,
        fontWeight: FontWeight.w600,
        color: AppColors.textMuted,
      ),
    );
  }
}

class _ActivityRow extends StatelessWidget {
  const _ActivityRow({
    required this.activity,
    required this.onEdit,
    required this.onActivate,
    required this.onDeactivate,
    required this.onDelete,
  });

  final Activity activity;
  final VoidCallback onEdit;
  final VoidCallback onActivate;
  final VoidCallback onDeactivate;
  final VoidCallback onDelete;

  @override
  Widget build(BuildContext context) {
    final canEdit = activity.allows(ActivityActions.update);
    final canActivate = activity.allows(ActivityActions.activate);
    final canDeactivate = activity.allows(ActivityActions.deactivate);
    final canDelete = activity.allows(ActivityActions.delete);

    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 18, vertical: 12),
      child: Row(
        children: [
          Expanded(
            flex: 4,
            child: Row(
              children: [
                _Thumbnail(url: activity.imageUrl),
                const SizedBox(width: 10),
                Expanded(
                  child: Text(
                    activity.name,
                    overflow: TextOverflow.ellipsis,
                    style: const TextStyle(fontWeight: FontWeight.w600),
                  ),
                ),
              ],
            ),
          ),
          Expanded(flex: 2, child: Text(activity.activityTypeName)),
          Expanded(
            flex: 2,
            child: Text(
              activity.animalName?.trim().isNotEmpty == true ? activity.animalName! : '—',
              overflow: TextOverflow.ellipsis,
            ),
          ),
          Expanded(flex: 2, child: Text(formatDurationLabel(activity.duration))),
          Expanded(flex: 2, child: Text(formatMoney(activity.price))),
          Expanded(flex: 2, child: Text('${activity.capacity}')),
          Expanded(
            flex: 2,
            child: Align(
              alignment: Alignment.centerLeft,
              child: _statusChip(activity.status),
            ),
          ),
          SizedBox(
            width: 156,
            child: Row(
              children: [
                _IconAction(
                  icon: Icons.edit_outlined,
                  color: AppColors.textMuted,
                  tooltip: canEdit ? 'Edit' : _disabledTooltip(ActivityActions.update),
                  enabled: canEdit,
                  onPressed: onEdit,
                ),
                const SizedBox(width: 4),
                _IconAction(
                  icon: Icons.play_arrow_outlined,
                  color: AppColors.green,
                  tooltip: canActivate ? 'Activate' : _disabledTooltip(ActivityActions.activate),
                  enabled: canActivate,
                  onPressed: onActivate,
                ),
                const SizedBox(width: 4),
                _IconAction(
                  icon: Icons.pause_outlined,
                  color: AppColors.warningText,
                  tooltip: canDeactivate
                      ? 'Deactivate'
                      : _disabledTooltip(ActivityActions.deactivate),
                  enabled: canDeactivate,
                  onPressed: onDeactivate,
                ),
                const SizedBox(width: 4),
                _IconAction(
                  icon: Icons.delete_outline,
                  color: AppColors.danger,
                  tooltip: canDelete ? 'Delete' : _disabledTooltip(ActivityActions.delete),
                  enabled: canDelete,
                  onPressed: onDelete,
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  String _disabledTooltip(String action) {
    switch (action) {
      case ActivityActions.update:
        if (activity.status == ActivityStatus.active) {
          return 'Deactivate an active activity before editing it.';
        }
        return 'Only a draft can be edited.';
      case ActivityActions.activate:
        return 'This activity is already active.';
      case ActivityActions.deactivate:
        if (activity.status == ActivityStatus.draft) {
          return 'A draft is activated, not deactivated.';
        }
        return 'This activity is already inactive.';
      case ActivityActions.delete:
        if (activity.status == ActivityStatus.active) {
          return 'An active activity cannot be deleted. Deactivate it first.';
        }
        return 'Only a draft can be deleted.';
      default:
        return 'This action is not allowed.';
    }
  }
}

Widget _statusChip(ActivityStatus status) {
  switch (status) {
    case ActivityStatus.active:
      return const StatusChip(active: true, label: 'Active');
    case ActivityStatus.inactive:
      return const StatusChip(active: false, label: 'Inactive');
    case ActivityStatus.draft:
      return const StatusChip(
        active: false,
        label: 'Draft',
        background: Color(0xFFE8E6E1),
        foreground: AppColors.textMuted,
      );
  }
}

class _Thumbnail extends StatelessWidget {
  const _Thumbnail({required this.url});

  final String? url;

  @override
  Widget build(BuildContext context) {
    final image = url?.trim();
    return ClipRRect(
      borderRadius: BorderRadius.circular(8),
      child: SizedBox(
        width: 42,
        height: 42,
        child: image == null || image.isEmpty
            ? const ColoredBox(
                color: AppColors.border,
                child: Icon(Icons.image_outlined, size: 18, color: AppColors.textMuted),
              )
            : Image.network(
                image,
                fit: BoxFit.cover,
                errorBuilder: (_, _, _) => const ColoredBox(
                  color: AppColors.border,
                  child: Icon(Icons.broken_image_outlined, size: 18, color: AppColors.textMuted),
                ),
              ),
      ),
    );
  }
}

class _IconAction extends StatelessWidget {
  const _IconAction({
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
          child: Icon(
            icon,
            size: 18,
            color: enabled ? color : color.withValues(alpha: 0.35),
          ),
        ),
      ),
    );
  }
}

class _ErrorState extends StatelessWidget {
  const _ErrorState({required this.message, required this.onRetry});

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

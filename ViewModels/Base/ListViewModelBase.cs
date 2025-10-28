using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;

namespace AutoScheduling3.ViewModels.Base;

/// <summary>
/// 列表 ViewModel 基类 - 提供列表管理的通用功能
/// </summary>
public abstract class ListViewModelBase<T> : ViewModelBase where T : class
{
    private T? _selectedItem;
    private string _searchKeyword = string.Empty;

    /// <summary>
    /// 数据项列表
    /// </summary>
    public ObservableCollection<T> Items { get; } = new();

    /// <summary>
    /// 当前选中的项
    /// </summary>
    public T? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (SetProperty(ref _selectedItem, value))
            {
                OnSelectedItemChanged(value);
            }
        }
    }

    /// <summary>
    /// 搜索关键字
    /// </summary>
    public string SearchKeyword
    {
        get => _searchKeyword;
        set
        {
            if (SetProperty(ref _searchKeyword, value))
            {
                _ = SearchAsync();
            }
        }
    }

    /// <summary>
    /// 刷新命令
    /// </summary>
    public IAsyncRelayCommand RefreshCommand { get; }

    /// <summary>
    /// 搜索命令
    /// </summary>
    public IAsyncRelayCommand SearchCommand { get; }

    protected ListViewModelBase()
    {
        RefreshCommand = new AsyncRelayCommand(LoadDataAsync);
        SearchCommand = new AsyncRelayCommand(SearchAsync);
    }

    /// <summary>
    /// 加载数据
    /// </summary>
    public abstract Task LoadDataAsync();

    /// <summary>
    /// 搜索数据
    /// </summary>
    protected virtual async Task SearchAsync()
    {
        await LoadDataAsync();
    }

    /// <summary>
    /// 选中项变更事件处理
    /// </summary>
    protected virtual void OnSelectedItemChanged(T? newItem)
    {
        // 子类可以重写此方法来处理选中项变更
    }

    /// <summary>
    /// 清空列表
    /// </summary>
    protected void ClearItems()
    {
        Items.Clear();
        SelectedItem = null;
    }

    /// <summary>
    /// 添加项到列表
    /// </summary>
    protected void AddItem(T item)
    {
        Items.Add(item);
    }

    /// <summary>
    /// 从列表移除项
    /// </summary>
    protected bool RemoveItem(T item)
    {
        return Items.Remove(item);
    }

    /// <summary>
    /// 批量添加项
    /// </summary>
    protected void AddRange(IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            Items.Add(item);
        }
    }
}

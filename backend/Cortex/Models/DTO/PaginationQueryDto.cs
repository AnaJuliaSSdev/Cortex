using System.ComponentModel.DataAnnotations;

namespace Cortex.Models.DTO;

public class PaginationQueryDto
{
    private const int MaxPageSize = 50;
    private int _pageSize = 10;

    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;

    [Range(1, MaxPageSize)]
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = (value > MaxPageSize) ? MaxPageSize : value;
    }
}

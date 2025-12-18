using System;
using System.Collections.Generic;

namespace M4Food.Models.DTOs;

/// <summary>
/// DTO for caching orders locally
/// </summary>
public class OrderCacheDto
{
    public string OrderId { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public double TotalPrice { get; set; }
    
    public List<OrderItemCacheDto> Items { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? LastSyncedAt { get; set; }
    public bool NeedsSync { get; set; } = false;
}

public class OrderItemCacheDto
{
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public double Price { get; set; }
    public string StoreName { get; set; } = string.Empty;
}


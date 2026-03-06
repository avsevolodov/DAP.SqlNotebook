
using System;

namespace DAP.SqlNotebook.UI.Models;

public class DbConnectionInfo
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string ConnectionString { get; set; } = "";
}

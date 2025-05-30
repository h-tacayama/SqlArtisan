﻿# SqlArtisan.TableClassGen

A .NET tool that generates C# table schema classes from your database for `SqlArtisan`, enabling robust type-safety and IntelliSense in your query building.

## Installation

Install `sa-tableclassgen` as a .NET global tool to make it accessible from any location:

*(Note: These packages are currently in their pre-release phase, so use the --prerelease flag when installing.)*

```bash
dotnet tool install --global SqlArtisan.TableClassGen --prerelease
```

## Usage
The command to invoke the tool is:

```bash
sa-tableclassgen
```

## Example Execution

### Interactive Command Session

Here's an example of the interactive prompts when running the sa-tableclassgen command:

```
Please enter database information.
Database type (1.Oracle/2.PostgreSQL): 2
Host: localhost
Port: 5432
Service name (or database name): myservice
Schema: myschema
Username: myuser
Password: *****

Please enter code generation settings.
Namespace: MyAppNamespace
Convert object names to lowercase (y/n): y
Output directory: c:\output
Create subfolders by table name initial (y/n): y
Specific table name (leave empty for all tables):

Retrieving all tables from database...
Found 17 tables. Generating class files...
Successfully generated 17 out of 17 table classes.
Table class generation process completed successfully.
```

### Output: Example Table Schema Class
As a result of the command, a C# class similar to the following would be generated (e.g., for a 'category' table):

```csharp
using SqlArtisan;

namespace MyAppNamespace;

internal sealed class UsersTable : DbTableBase
{
    public UsersTable(string tableAlias = "") : base("users", tableAlias)
    {
        Id = new DbColumn(tableAlias, "id");
        Name = new DbColumn(tableAlias, "name");
        CreatedAt = new DbColumn(tableAlias, "created_at");
    }

    public DbColumn Id { get; }
    public DbColumn Name { get; }
    public DbColumn CreatedAt { get; }
}
```

## License

This project is licensed under the MIT License. See the [LICENSE](https://github.com/h-tacayama/SqlArtisan/blob/main/LICENSE) file for the full license text.

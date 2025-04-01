using InlineSqlSharp.TableClassGen;

TableClassGenUseCase useCase = new();
useCase.Execute();

Console.WriteLine("Press any key to exit...");
Console.ReadKey();
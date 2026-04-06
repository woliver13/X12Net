# X12Net

A lightweight, object-oriented **ANSI ASC X12 EDI parser for .NET**.

X12Net provides a flexible way to parse, transform, and work with X12 transaction sets in C#, enabling developers to convert EDI documents into structured formats such as XML, HTML, or other custom representations.

---

## 🚀 Features

* 📦 **X12 Parsing Engine**
  Parse ANSI ASC X12 EDI documents into an object model.

* 🧱 **Object-Oriented Design**
  Work with strongly structured hierarchical data (Interchange → Groups → Transactions → Segments).

* 🧩 **Extensible Architecture**
  Define custom transaction sets and extend parsing behavior.
---

## 📚 What is X12?

X12 (ANSI ASC X12) is a widely used **Electronic Data Interchange (EDI)** standard for exchanging structured business data across industries like healthcare, finance, logistics, and retail.

This library helps translate raw X12 text into usable application data.

---

## 🛠️ Installation

Clone the repository:

```bash
git clone https://github.com/woliver13/X12Net.git
```

Open the solution in Visual Studio:

```
X12Net.sln
```

Build the project:

```bash
dotnet build
```

---

## ⚡ Quick Start

### Parse an X12 File

```csharp
using X12Net;

var parser = new X12Parser();
var document = parser.Parse("sample.edi");

// Access hierarchical data
foreach (var group in document.FunctionGroups)
{
    foreach (var transaction in group.Transactions)
    {
        Console.WriteLine(transaction.Header.TransactionSetIDCode);
    }
}
```

---

## 🧩 Project Structure

```
/src        → Core parsing logic
/tests      → Unit tests
X12Net.sln     → Solution file
```

---

## 🏗️ Architecture Overview

X12Net models EDI documents using a hierarchical structure:

```
Interchange (ISA/IEA)
 └── Functional Group (GS/GE)
      └── Transaction Set (ST/SE)
           └── Segments
                └── Elements
```

This mirrors the official X12 specification and allows flexible traversal and transformation of documents.

---

## 🔧 Extending the Parser

You can define custom transaction sets and mapping logic:

* Add new segment definitions
* Customize parsing rules
* Map segments to domain models

---

## 🧪 Testing

Run tests using:

```bash
dotnet test
```

---

## 🤝 Contributing

Contributions are welcome!

1. Fork the repo
2. Create a feature branch
3. Submit a pull request

Please include tests and clear documentation for any changes.

---

## 📄 License

This project is licensed under the terms of the license included in the repository.

---

## 🙏 Acknowledgments

* Inspired by earlier X12 parser implementations and open-source EDI tooling
* Built to simplify working with legacy EDI formats in modern .NET applications

---

## 📬 Support

If you encounter issues or have questions:

* Open an issue on GitHub
* Provide sample X12 files where possible
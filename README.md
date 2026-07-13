# 🛍️ Local AI Shopping Agent (Ollama + Semantic Kernel)

An offline, privacy-focused, **agentic AI shopping assistant** built with .NET 8 and Microsoft's **Semantic Kernel**. This project utilizes the **ReAct (Reasoning + Acting)** pattern, allowing a locally hosted Large Language Model (via Ollama) to autonomously choose, configure, and execute native C# tools to handle inventory searches and discount calculations.

---

## 🚀 Features

* **100% Offline & Private:** No external cloud API calls or data leaks. All reasoning happens locally on your hardware.
* **True Agentic Workflow:** Uses semantic tool-calling. The model autonomously determines *when* to search the catalog or apply business logic based on user intent.
* **Native C# Plugin Integration:** Bridges the LLM text engine directly to structural C# code, records, and data validation layers.
* **Robust Connection Resilience:** Configured with an extended HttpClient execution loop to handle heavy local model load-times smoothly.

---

## 🛠️ Architecture & Tools Breakdown

The agent has access to a compiled C# execution suite via reflection attributes (`[KernelFunction]`):

| Tool Name | Parameters | Core Responsibility |
| :--- | :--- | :--- |
| `SearchCatalog` | `query` *(string)* | Scans product titles and category groupings to return real-time matching inventory objects in JSON format. |
| `ApplyDiscount` | `cartTotal` *(decimal)*, `promoCode` *(string)* | Executes safe financial subtractions locally to apply valid promotional rules (e.g., `SAVE10`, `WELCOME5`). |

---

## 📋 Prerequisites

1. **.NET 8 SDK** installed on your system.
2. **Ollama** installed and running locally.
3. A tool-calling capable local model pulled in Ollama (highly recommended: `llama3.1` or `qwen2.5` variants):

```bash
ollama pull llama3.1

# Intelligent Fact-Grounded RAG System

This project is a **future-ready intelligent system** that dynamically builds and updates knowledge from live URLs (HTML & PDF) into a **vector database**, enabling **fact-based**, **real-time responses** powered by **Retrieval-Augmented Generation (RAG)**.

---

## 🧠 Main Architecture Flow

```plaintext
.NET Core Web API 
    ↓
MCP Client 
    ↓
MCP Server (for embedding generation)
    ↓
Qdrant Vector Database (stores embeddings + metadata)
    ↓
RAG (Retrieval from Qdrant)
    ↓
LLM (Large Language Model generates user response)
    ↓
User
```

---

## 🚀 Key Concepts

- **Dynamic Knowledge Updates**  
  Scrape new URLs anytime (HTML or PDF) → Parse → Embed → Save into Qdrant without system downtime.

- **MCP-Based Embedding Generation**  
  Use **Model Context Protocol (MCP)** clients to communicate with LLM servers for embedding documents efficiently.

- **Fact-Grounded Responses**  
  Instead of hallucinating answers, the system **retrieves actual facts** stored in vectors to generate responses.

- **Scalable and Future-Proof**  
  Modular components (Web API, MCP Client, Qdrant, RAG, LLM) allow swapping or upgrading technologies easily.

- **Metadata Preservation**  
  Each document vector stores not just embeddings but also critical metadata (e.g., URL, title, source type, scraped timestamp) for better retrieval and traceability.

---

## 📚 How It Works

1. **User provides a list of URLs**  
   (Web pages or PDFs).

2. **Scraper Service**  
   Downloads and extracts the raw content.

3. **Document Parser Service**  
   Cleans the content depending on file type (HTML or PDF).

4. **Embedding Generation**  
   Content is sent to an **MCP Server** to generate numerical vector representations (embeddings).

5. **Vector Store Service**  
   Embeddings + metadata are stored into **Qdrant Vector DB**.

6. **User Query (RAG Flow)**  
   - User asks a question.  
   - The system queries Qdrant to find the most relevant document chunks.  
   - Retrieved chunks are passed into the **LLM** as context.  
   - The LLM answers based on real retrieved information — not guesses.

---

## 🔮 Why This Matters

- **Traditional LLMs** make up (hallucinate) information.  
- **Our system** retrieves real documents and augments LLMs, ensuring **trustworthy**, **verifiable**, and **updatable** answers.  
- This architecture represents the **future of responsible AI**: **dynamic, modular, factual, and constantly learning**.

---

## 🛠️ Technologies Used

- **.NET Core Web API**  
- **Model Context Protocol (MCP)**  
- **Qdrant Vector Database**  
- **Large Language Models (LLMs)**  
- **Scraper (HTML/PDF Parsing)**  
- **Newtonsoft.Json**, **HttpClient**, **MediatR**, and more

---

## 📈 Future Enhancements (Vision)

- Support **multi-language documents** scraping and embedding.  
- Enable **real-time ingestion pipelines** (streaming URLs).  
- Plug-in different LLM providers via MCP.  
- Auto-refresh documents on schedule to keep vectors always up-to-date.  
- Build a **user-friendly dashboard** to manage knowledge base easily.

---

## 📝 License & Usage

This project is licensed under the **Apache License 2.0**.

You're free to:  
✅ Use it commercially or personally  
✅ Modify, fork, and redistribute it  
✅ Build your own projects with it  

As long as you:  
- Include a copy of the **Apache 2.0 License**  
- **Give attribution** to this project  
- **Disclose changes** if you modify it  
- Don’t use the authors’ names or brand for promotion without permission

### How to Use:
1. Fork or clone this repo  
2. Build your solution based on the architecture  
3. Keep the `LICENSE` file intact  
4. Add attribution like:

> “Built with components from the [Intelligent Fact-Grounded RAG System](https://github.com/your-repo-url) (Apache 2.0)”  

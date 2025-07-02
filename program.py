import subprocess
import pandas as pd
import tkinter as tk
from tkinter import messagebox, ttk, filedialog
import shutil
import os
import webbrowser
import spotipy
from spotipy.oauth2 import SpotifyClientCredentials

# Autentifikácia pomocou Spotify API (Client ID + Secret)
sp = spotipy.Spotify(auth_manager=SpotifyClientCredentials(
    client_id="id",
    client_secret="secret"
))

# Funkcia na výber CSV súboru a jeho skopírovanie do projektu v C#
def choose_and_copy_custom_csv():
    filepath = filedialog.askopenfilename(title="Select your CSV dataset", filetypes=[("CSV Files", "*.csv")])
    if filepath:
        try:
            destination_csharp = "/Users/mac/c#/k/charp_code/spotify.csv"
            if os.path.exists(destination_csharp):
                os.remove(destination_csharp)
            shutil.copy(filepath, destination_csharp)
            print("Súbor skopírovaný do C# modulu ako spotify.csv.")
            messagebox.showinfo("Súbor vybraný", "Dataset bol odoslaný do C# modulu.")
            return True
        except Exception as e:
            messagebox.showerror("Chyba", f"Nepodarilo sa skopírovať súbor: {e}")
            return False
    return False

# Konfigurácia grafického používateľského rozhrania (GUI)
root = tk.Tk()
root.title("🎵 Music Recommender")
root.geometry("1500x1000")
root.configure(bg="#2C2F33")

# Definícia štýlov (farby, fonty)
FONT = ("Segoe UI", 12)
TITLE_FONT = ("Segoe UI", 16, "bold")
FG = "#ffffff"
BG = "#2C2F33"
ACCENT = "#7289DA"

# Názov nad filtrom
title_label = tk.Label(root, text="🎼 Vyber si preferované hudobné štýly:", font=TITLE_FONT, fg=FG, bg=BG)
title_label.pack(pady=15)

# Tlačidlo na nahratie CSV súboru a spustenie generovania + načítania
def upload_dataset():
    if choose_and_copy_custom_csv():
        generate_dataset()
        load_dataset()

upload_btn = tk.Button(root, text="📂 Nahraj svoj dataset", command=upload_dataset,
                       font=("Segoe UI", 12), bg=ACCENT, fg="white", padx=10, pady=5)
upload_btn.pack(pady=5)

# Spustenie C# algoritmu cez dotnet run
def generate_dataset():
    print("🚀 Spúšťam C# projekt...")
    try:
        subprocess.run(["dotnet", "run"], cwd="/Users/mac/c#/k/charp_code/", check=True)
        print("✅ C# dokončené, CSV súbor vygenerovaný!")
    except subprocess.CalledProcessError as e:
        print(f"❌ Chyba: {e}")
        messagebox.showerror("Chyba", "Nepodarilo sa spustiť C# kód.")
        exit()

# Načítanie datasetu vygenerovaného z C#
def load_dataset():
    global df
    CSV_PATH = "/Users/mac/c#/k/clustered_songs.csv"
    try:
        df = pd.read_csv(CSV_PATH, quotechar='"')
        df.columns = df.columns.str.strip()
        if "Cluster" not in df.columns:
            messagebox.showerror("Chyba", "Dataset neobsahuje stĺpec 'Cluster'.")
            print("⚠️ Stĺpce v datasete:", df.columns.tolist())
            return
    except FileNotFoundError:
        messagebox.showerror("Chyba", f"Súbor nenájdený: {CSV_PATH}")
        exit()
    except pd.errors.ParserError as e:
        messagebox.showerror("Chyba", f"Chyba pri parsovaní CSV: {e}")
        exit()

# Počiatočné načítanie
generate_dataset()
load_dataset()

# Názvy klastrov
CLUSTER_LABELS = {
    0: "🎧 Pokojné skladby",
    1: "🔥 Energetické hity",
    2: "💃 Tanečné skladby"
}

# Výber preferovaných klastrov (checkboxy)
cluster_vars = {}
for cluster, label in CLUSTER_LABELS.items():
    var = tk.BooleanVar()
    cluster_vars[cluster] = var
    tk.Checkbutton(root, text=label, variable=var, font=FONT, fg=FG, bg=BG, selectcolor=BG, activebackground=BG).pack(anchor="w", padx=20)

# Výber spôsobu triedenia výsledkov
sort_frame = tk.Frame(root, bg=BG)
sort_frame.pack(pady=10)
tk.Label(sort_frame, text="Zoradiť podľa:", font=FONT, fg=FG, bg=BG).pack(side="left")
sort_var = tk.StringVar(value="None")
tk.OptionMenu(sort_frame, sort_var, "None", "Title Ascending", "Title Descending", "Artist Ascending", "Artist Descending").pack(side="left")

# Funkcia na generovanie odporúčaní podľa výberu
def recommend_songs():
    selected_clusters = [cluster for cluster, var in cluster_vars.items() if var.get()]

    if not selected_clusters:
        messagebox.showwarning("⚠️ Upozornenie", "Vyber aspoň jednu kategóriu.")
        return

    if "Cluster" not in df.columns:
        messagebox.showerror("Chyba", "Dataset neobsahuje stĺpec 'Cluster'.")
        return

    recommended = df[df["Cluster"].isin(selected_clusters)]

    if recommended.empty:
        messagebox.showinfo("😕 Nenašli sa výsledky", "Skús iné nastavenia filtrov.")
        return

    sort_by = sort_var.get()
    if sort_by == "Title Ascending":
        recommended = recommended.sort_values(by="Title", ascending=True)
    elif sort_by == "Title Descending":
        recommended = recommended.sort_values(by="Title", ascending=False)
    elif sort_by == "Artist Ascending":
        recommended = recommended.sort_values(by="Artist", ascending=True)
    elif sort_by == "Artist Descending":
        recommended = recommended.sort_values(by="Artist", ascending=False)

    # Nové okno s odporúčaniami
    result_window = tk.Toplevel(root)
    result_window.title("🎶 Odporúčané skladby")
    result_window.geometry("600x500")
    result_window.configure(bg=BG)

    main_frame = tk.Frame(result_window, bg=BG)
    main_frame.pack(fill="both", expand=True)

    canvas = tk.Canvas(main_frame, bg=BG, highlightthickness=0)
    scrollbar = tk.Scrollbar(main_frame, orient="vertical", command=canvas.yview)
    scrollable_frame = tk.Frame(canvas, bg=BG)

    scrollable_frame.bind("<Configure>", lambda e: canvas.configure(scrollregion=canvas.bbox("all")))
    canvas.create_window((0, 0), window=scrollable_frame, anchor="nw")
    canvas.configure(yscrollcommand=scrollbar.set)
    canvas.pack(side="left", fill="both", expand=True)
    scrollbar.pack(side="right", fill="y")

    # Vytvorenie jednotlivých skladieb s odkazmi na Spotify
    for i, (_, row) in enumerate(recommended.iterrows(), start=1):
        text = f"{i}. 🎵 {row['Title']} - {row['Artist']}"
        label = tk.Label(scrollable_frame, text=text, font=FONT, fg=FG, bg=BG, anchor="w", cursor="hand2", pady=5)
        label.pack(fill="x", padx=10, pady=2)

        # Odkaz na skladbu cez Spotify API (ak sa nájde)
        def open_spotify(event, title=row['Title'], artist=row['Artist']):
            try:
                result = sp.search(q=f"track:{title} artist:{artist}", type="track", limit=1)
                if result['tracks']['items']:
                    url = result['tracks']['items'][0]['external_urls']['spotify']
                else:
                    query = f"{title} {artist}".replace(' ', '%20')
                    url = f"https://open.spotify.com/search/{query}"
                webbrowser.open(url)
            except:
                query = f"{title} {artist}".replace(' ', '%20')
                url = f"https://open.spotify.com/search/{query}"
                webbrowser.open(url)

        label.bind("<Button-1>", open_spotify)
        label.bind("<Enter>", lambda e, l=label: l.config(bg="#40444B"))
        label.bind("<Leave>", lambda e, l=label: l.config(bg=BG))

# Tlačidlo na spustenie odporúčaní
recommend_button = tk.Button(root, text="🎵 Získať odporúčania", command=recommend_songs,
                             font=("Segoe UI", 14, "bold"), bg=ACCENT, fg="white", padx=20, pady=10)
recommend_button.pack(pady=25)

# Spustenie GUI aplikácie
root.mainloop()

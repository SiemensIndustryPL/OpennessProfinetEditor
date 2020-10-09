# OpennessProfinetEditor

Aplikacja TIA Openness od szybkiej edycji sieciowych parametrów urządzeń.


## Instalacja

Program można pobrać klonując to repozytorium i kompilując je w Visual Studio, lub pobierając stąd gotowy plik wykonywalny.
Plik wykonywalny korzysta z biblioteki Openness w wersji 16, ale projekt powinien dać się skompilować także pod 15.1.
Po kompilacji sklonowanego projektu, plik NetEditor.exe powinien być dostępny w katalogu `NetEditor\bin\Release\`.

W TIA Portalu, przejdź do `Tools` -> `External applications` -> `Configure...`

![Tools -> External applications -> Configure...](img/Readme_1.png)

Wprowadź ścieżkę do NetEditor.exe przy polu `Command:`. 

![Add new external application](img/Readme_2.png)

## Używanie

Aby uruchomić OpennessProfinetEditor, przejdź do `Tools` -> `External applications` -> `NetEditor`.

W tabeli urządzeń można edytować każdą aktywną komórkę. Zmiany są przenoszone do projektu TIA Portal, ale nie są finalne, dopóki nie klikniesz `Commit Changes`. Nawet po dokonaniu zmian w projekcie, wszystkie zmiany mogą być cofnięte w TIA Portalu przez funkcję `undo` (lub `ctrl+z`).

Czerwone tło pod adresem IP oznacza, że ten adres został już użyty w innym miejscu w projekcie.

Pole statusu rejestruje wszystkie zmiany wykonane w konfiguracji, oraz informuje o innych wydarzeniach.

![NetEditor window](img/Readme_3.png)

### Przyciski

`Connect` / `Refresh` : połącz z otwartym projektem TIA Portal, lub odśwież go, jeśli jakieś zmiany nie zostały zawarte w tabeli.
Aby się połączyć, musisz zezwolić na to w wyskakującym w TIA Portalu oknie. TIA Portal przejdzie w tryb wyłącznego dostępu (Exclusive Access) i tylko OpennessProfinetEditor będzie mógł wtedy wprowadzać zmiany do projektu.

`Disconnect` : odrzuć wszystkie niedokonane zmiany i odłącz OpennessProfinetEditor od projektu TIA Portal. Oddaje dostęp TIA Portalowi.

`Commit Changes` : zmiany wprowadzone w tabeli nie są ostateczne dopóki nie zostaną  potwierdzone tym przyciskiem.

`Clear Changes` : czyści zmiany wprowadzone w tabeli. Wymaga odświeżenia tabeli.

`Export to CSV` : tworzy plik `.csv` _na podstawie tabeli_.

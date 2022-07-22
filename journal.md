### 2022-07-22

* Modifir le système de gestion des erreurs.

* Continuer le travail sur [manuel-tiny-BASIC.md](manuel-tiny-BASIC.md)

### 2022-07-21

* Continuer la rédaction de [manuel-tiny-BASIC.md](manuel-tiny-BASIC.md)

* réparer bogue dans PRINT 

* Corrigé bogue dans routine *factor* , après l'appel à *expect-)* DROP 2DROP étaient en trop. 

* Réparer bogue dans *expression*,  ajouté  **$FFFF AND sign-extend** pour ajuster l'entier retourné. 

### 2022-07-20

* initialisation git 

* création de [readme.md](readme.md)

* bogue devrai afficher -1 
```
#let a=-1 ; ? a 
 65535

#

```

* Autre bogue
```
#? -1 

? -
in file included from *OS command line*:-1
tiny-basic.fs:1547: error 8 
>>>BASIC<<<
Backtrace:
$7F03CEB908E8 throw 
$7F03CEB95748 error 
$FFFFFFFFFFFFFFFF 
$7F03CEB939B8 execute 
$7F03CEB96178 line-eval 
$7F03CEB96218 cmd-eval 
$7F03CEB962C8 CMD-LINE 
```

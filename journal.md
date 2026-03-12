### 2022-10-14 

* renommé répertoire **premiers-pas** en **lib** et déplacé fichier [xorshift.fs](lib/xorshift64.fs) dans **lib**.

* Modifié documentation dans [xorshift.fs](lib/xorshift64.fs).

* Modifié référence à [xorshift.fs](lib/xorshift64.fs) dans les programmes BASIC.


### 2022-09-29

* Amélioration de la commande **DIR** en envoyant la sortie de **ls** via un **pipe**  vers **grep**.

### 2022-09-28

* Ajout de la commande **DIR** au programme [tiny-BASIC/tiny-basic.fs](tiny-basic.fs)

### 2022-07-26

* Modifié tiny-basic.fs pour remplacé **random.fs** par [xorshift64.fs](xorshift64.fs) 
  et modifié autre code.
  
* Création de [xorshift64.fs](xorshift64.fs) en remplacement de **random.fs** qui est un médiocre PRNG.

* Corrigé bogue dans snake.fs

### 2022-07-25 

* Découvert un bogue dans snake.fs parfois la pastille apparaît en dehors de l'ère de jeu.

* Création d'un dossier pour chaque programme Forth.

* Complété le programme [tb-test.bas](tb-test.bas)

* Travail sur [manuel-tiny-BASIC.md](manuel-tiny-BASIC.md)

* Modifié *loopback-save*.

* Ajouté **THEN** à la commande **IF**. 
 

* Corrigé erreur dans *error*

* Corrigé erreur dans *factor*, le signe du résultat n'était pas étendu à 64 bits.

* Modifié commande **LIST** en ajoutant la définition *list-line* mise en facteur avec *error*

### 2022-07-24 

* Autre bogue FOR...NEXT ne fonctionne pas sur multiligne.
```
#list 
   10 for i=1 to 10
   20 ? i,
   30 next i

#run 
     1
#new 

#10 for i=1 to 10 ; ? i, ; next i 

#list 
   10 for i=1 to 10 ; ? i, ; next i

#run 
     1     2     3     4     5     6     7     8     9    10
#

```

* Ce bogue est maintenant corrigée.

* Les modifications à *add-line* et compagnie ont introduit un bogue, insertion et effacement de ligne ne fonctionne plus.
```
#10 for i=1 

#20 ? i,  

#30 next i 

#run 
     1     2     3     4     5     6     7     8     9    10
#25 rem fdas 

#list 
   10 for i=1
   20 ? i, 
   30 next i
   25 rem fdas

#25    

#list 
   10 for i=1
   20 ? i, 
   30 next i
   25 
   25 rem fdas
```

* Nombreuses modfication au code source.

	* Maintenant les GOSUB utilise la même pile que les FOR...NEXT. Elle a été renommée *cstack*. 

	* Corrigé bogues


### 2022-07-22

* Trouvé bogue en exécutant [kingdom.bas](kingdom.bas)
```
THE KINGDOM OF EUPHORIA


erreur: -9 
 3510 LET Y=RND(6)+1
                    ^

#

```
* Autre bogue
```
#new 

#10 let y=rnd(6)+1 

#run 

erreur: 1 
GDOM
    ^
Erreur de syntaxe.
#


```

* Ajouter vocabulaire 	**WLIST-TBASIC** pour limiter la recherche des commandes aux mots de l'application.

* Corrigé bogue dans *error* qui n'affichait pas le numéro de ligne.

### 2022-07-21

* Modifir le système de gestion des erreurs.

* Continuer le travail sur [manuel-tiny-BASIC.md](manuel-tiny-BASIC.md)

### 2022-07-20

* Continuer la rédaction de [manuel-tiny-BASIC.md](manuel-tiny-BASIC.md)

* réparer bogue dans PRINT 

* Corrigé bogue dans routine *factor* , après l'appel à *expect-)* DROP 2DROP étaient en trop. 

* Réparer bogue dans *expression*,  ajouté  **$FFFF AND sign-extend** pour ajuster l'entier retourné. 

### 2022-07-19

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

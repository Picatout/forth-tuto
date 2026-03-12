### Tiny BASIC

Ce dossier contient l'implémentation en **gforth** d'un dialecte tiny BASIC. Il s'agit d'un simple exercice de programmation en Forth. Je n'ai pas essayé d'optimiser l'interpréteur. Je me suis concentrer sur la clarté du code source. 

Le dossier contient aussi quelques programmes BASIC pour tester l'interpréteur.

Le fichier [TINIDISK.DOC](TINIDISK.DOC) m'a servie de référence pour le langage quoique ma version n'est pas tout à fait inditique à celle décrite dans ce document. Il faut aussi consulter l'article Wikipedia (en anglais) consacré à [Tiny BASIC](https://en.wikipedia.org/wiki/Tiny_BASIC).


### Lancer tiny BASIC
sur la ligne de commande
```
gforth tiny-basic.fs
```
### Pour executer un programme tiny BASIC
run nom_du_programme. Exemple:
#RUN fibo.bas 
chargement de fibo.bas
     1     1     2     3     5     8    13    21    34    55    89   144   233   377   610   987  1597  2584  4181  6765 10946 17711 28657
#list 

    1 rem Fibonacci
   10 let a=1,b=1
   20 ? a,
   30 let c=a+b,a=b,b=c
   40 if a<0 then end
   50 goto 20
```
commande __quit__ pour quitter tiny BASIC et retomber dans gForth.

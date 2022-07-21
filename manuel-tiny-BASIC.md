<!-- 
Copyright Jacques Deschênes, 2022
-->

# Manuel de l'utilisateur du Tiny BASIC que j'ai écris en gforth.

Pour écris cet interpréteur Tiny BASIC je me suis inspiré du manuel [TINIDISK.DOC](TINYDISK.DOC) bien que les fonctionnalités ne sois pas tout fait indentiques.
A l'origine Tiny BASIC a été créé au début des années pour les ordinateurs 8 bits de l'époque qui étaient limitées en mémoire et en puissance. J'ai donc omis les fonctionnalitées qui
ne s'applique pas à cette version qui  fonctionne sur un PC moderne. 

### fonctions non répliquées

* **USR** 

* **INP**

* **WAIT**

* **PEEK** 

* **POKE**

### fonctions ajoutées

* **PAUSE** *expr*  pour suspendre l'exécution d'un programme pendant un certain nomnbre de millisecondes

* J'ai ajout l'alias **?** pour la commande **PRINT**

* La commande **PRINT** peut envoyer des caractères de contrôles en utilisant un backslash comme ceci **\27** envoie le code ESC au terminal.

* J'ai ajouter l'opérateur **%** qui retourne le modulo d'une division.

* **NOT** *relation* effectue une négatiopn booléenne de la relation en argument.

### Implémentation 

Il s'agit d'un interpréteur pur, aucune compilation en jetons n'est faite avant l'exécution. Cependant avec la puissance des PC modernes l'exécution est beaucoup plus rapide que sur les ordinateurs 
8 bits des années 70-80. Je n'ai fait aucun effort d'optimisation de la performance car il ne s'agit que d'un exercice pour me familiariser avec **gforth**. 

### type de données 

* le seul type de donnée est l'entier 16 bits donc dans l'intervalle {-32768..32767}. Cependant les commandes **PRINT** et **INPUT** permettent d'imprimer des chaînes de caractères.

* Les chaînes de caractères sont encadrées par des guillemets **"** ou des apostrophes **'** . Cependant le même caractère doit-être utilisé au début et à la fin.

### Opérateurs arithmétiques et précédences

La précédence des opérateurs est la même que dans la majorité des langages. En odre décroissant de priorité

* Les expressions entre parenthèses sont évaluées dans leur entièretée et ont donc une priorité supérieure au opérateur multiplilcatifs.

** **-** le moins unaire 

* __*__,__/__,__%__  Mulitplication, division, modulo ont la même priorité et sont évaluer de gauche à droite.

* **+**,**-**  Addition, soustraction ont la même priorité et sont évaluées de gauche à droit.


### Variables 

* Il n'y a que 26 variables de disponibles et elles portent le nom des lettres de l'alphabet {A..Z}

* Un tableau à une dimension appellé **@** est aussi disponible  et peut contenir 256 éléments. Il est indicé de {0..255}


### Opérateurs relationnels 

Pour la commande **IF** il existe des opérateurs relationnel qui établissent la relation entre 2 expressions.

* **=**  La relation est vrai si l'expression à gauche est égale à l'expression à droite.

* **&gt;** La realtion est vrai si l'expression de gauche est plus grande que celle de droite.

* **&gt;=** La relation est vrai si l'expression de gauche est plus grande ou égale à celle de droite.

* **&lt;** la relation est vrai si l'expression de gauche est plus petite que celle de droite.

* **&lt;=** La relation est vrai si l'expression de gauche et plus petite ou égale à celle de droite.

* **&gt;&lt;** La relation est vrai seulement si les 2 expressions ne sont pas égales.


### fonctions disponibles

* **ABS(expr)** retourne la valeur absolue de *expr*

* **KEY** Attend une touche du clavier, retourne le caractère

* **KEY?** Retourne vrai si un caractère est disponible du terminal.

* **NOT relation**  Retourne la négation booléenne de la relation. 

* **RND(expr)** retourne un entier aléatoire dans l'intervalle {1..expr}

### Commandes BASIC

* **END** termine l'exécution du programme

* **FOR** Initialise une boucle **FOR var=expr TO limit [STEP expr] [;déclaration]* ; NEXT var**

* **GOSUB line#** Fait un saut à une sous-routine débutant la la ligne *line#*.

* **GOTO line#** Fait un saut vers la ligne *line*

* **IF relation  ; [déclaration]*  Si la relation retourne vrai les déclaration qui suivent sur la même ligne sont exécuté. Sinon l'écution continue sur la ligne suivante.
```
```

* __INPUT [chaine]var [,[chaine]var]*__  est utilisé pour permettre à l'utilisateur de saisir une valeur. Cette valeur est déposée dans la variable. Si une chaîne est fournie avant la variable
la chaîne est affichée à l'écran, sinon c'est le nom de la variable qui est affichée. Plusieurs saisie peuvent-être effectuées en séparant les variables par une virgule.

* __LET var=relation [, var=relation]*__  permet d'assignée une valeur à une variable. Plusieurs assignement peuvent-être faites dans la même commande en les séparant par une virgule.

* **LIST [line#]** Affiche le listing du programme qui est en mémoire. Si un numéro de ligne est fourni, le listing débute à cette ligne.

* **LOAD nom-fichier** Charge en mémoire un fichier programme. 

* **NEW** Nettoye la mémoire programme pour la préparer à recevoir un nouveau programme.

* **NEXT var** Se place à la fin d'une boucle **FOR** Cette commande incrémente la variable de contrôle et boucle si la limite n'a pas encore étée atteinte. Lorsque la limite est dépasser la boucle se temrine et
l'exécution se poursuit à la commande suivante.

* **PAUSE expr** Suespend l'exécution pour un nombre de millisecondes.

* **PRINT ** 

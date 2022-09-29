\ tiny BASIC 
\ Copyright Jacques Deschênes 2022 
\ This file is part of tiny BASIC 
\
\     tiny BASIC is free software: you can redistribute it and/or modify
\     it under the terms of the GNU General Public License as published by
\     the Free Software Foundation, either version 3 of the License, or
\     (at your option) any later version.
\
\     tiny BASIC is distributed in the hope that it will be useful,
\     but WITHOUT ANY WARRANTY; without even the implied warranty of
\     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
\     GNU General Public License for more details.
\
\     You should have received a copy of the GNU General Public License
\     along with tiny BASIC.  If not, see <http://www.gnu.org/licenses/>.
\



MARKER forget-basic 

require ../xorshift64.fs

get-order 
get-current value current-org
wordlist set-current 
get-current swap 1+ set-order


\ valeur minimale d'un entier 16 bits
-32768 CONSTANT MIN-INT
\ valeur maxiamle d'un entier 16 bits
32767 CONSTANT MAX-INT
\ masque pour entier non signé de 16 bits
$FFFF CONSTANT U16-MASK  
\ Si le bit 15 == 1
\ étend le signe de l'entier sur 
\ sur le nombre de bits des entiers
\ du système, i.e 32 ou 64 bits. 
: sign-extend ( u16 -- n )
	DUP MAX-INT U> IF
		-1 U16-MASK - OR 
	ENDIF 
;

\ empile un entier de 16 bits 
\ à partir de la mémoire 
\ paramètres:
\ 	addr  addresse à lire
\ sortie:
\     n   entier lue
: I@ ( addr -- n )
	DUP C@ 256 * 
	SWAP 1+ C@ +
; 

\ dépose un entier 16 bits en mmémoire 
\ paramètres:
\	n		entier à déposer
\	addr	addresse destination
: I! ( n addr -- )
	SWAP sign-extend SWAP 
	DUP 2 PICK 
	8 RSHIFT SWAP C!
	1+ C!
;

\ ***************************************
\ création d'autres mots forth
\ pour remplacer les définitions utilisées 
\ par tiny-basic
: FORR 
	POSTPONE FOR 
; IMMEDIATE COMPILE-ONLY

: TOO 
	POSTPONE TO 
; IMMEDIATE COMPILE-ONLY

: NEXTT 
	POSTPONE NEXT 
; IMMEDIATE COMPILE-ONLY


: IFF 
	POSTPONE IF 
; IMMEDIATE COMPILE-ONLY

' ABS ALIAS ABSS 

\ ***************************************

\ recherche le nom d'une commande 
\ dans le vocabulaire WLIST-BASIC
: FIND ( c-addr u -- 0 | xt 1 | xt -1 ) 
	get-current SEARCH-WORDLIST 
;

\ constantes identifiant le type d'erreur
-514 CONSTANT ERR-NOT-A-FILE
-28  CONSTANT ERR-USER-BREAK
-10 CONSTANT ERR-DIV0
-9 CONSTANT ERR-BAD-ADDR
0 CONSTANT ERR-NONE
1 CONSTANT ERR-SYNTAX
2 CONSTANT ERR-UNKOWN
3 CONSTANT ERR-RT-ONLY
4 CONSTANT ERR-CMD-LINE-ONLY
5 CONSTANT ERR-MISSING
6 CONSTANT ERR-NOT-LINE
7 CONSTANT ERR-MEM-FULL
9 CONSTANT ERR-QUIT 

\ identifiant d'unitées lexicales 
0 CONSTANT IDLEX-NULL	\ fin de texte 
1 CONSTANT IDLEX-LABEL	\ nom de commande ou fonction 
2 CONSTANT IDLEX-VAR	\ variables {A..Z} 
3 CONSTANT IDLEX-ARRAY	\ variable tableau @ 
4 CONSTANT IDLEX-INTEGER \ entier 16 bits
5 CONSTANT IDLEX-MUL	\ *   multiplication
6 CONSTANT IDLEX-DIV	\ /   division
7 CONSTANT IDLEX-MOD	\ %   modulo
8 CONSTANT IDLEX-ADD	\ +   addition
9 CONSTANT IDLEX-SUB	\ -   soustraction
10 CONSTANT IDLEX-EQU 	\ =   = égalité
11 CONSTANT IDLEX-LT  	\ <   plus petit
12 CONSTANT IDLEX-LE	\ <=  plus petit ou égal
13 CONSTANT IDLEX-GT	\ >   plus grand
14 CONSTANT IDLEX-GE 	\ >=  plus grand ou égal
15 CONSTANT IDLEX-NE	\ <>  différent 
16 CONSTANT IDLEX-LPAREN \ (  parenthèse gauche
17 CONSTANT IDLEX-RPAREN \ )  parenthèse droite
18 CONSTANT IDLEX-SHARP	\ #   dièse
19 CONSTANT IDLEX-COMMA \ ,   virgule 
20 CONSTANT IDLEX-SCOL \ ;    point virgule
21 CONSTANT IDLEX-QUOTE \ "   guillemets
22 CONSTANT IDLEX-BKSLASH \ \   barre oblique inversée

\ grandeur des entiers en octets ( 16 bits )
2 CONSTANT INTGR-SIZE

\ grandeur du tampon de saisie 
80 CONSTANT CMD-BUF-SIZE

\ tampon ligne de commande 
CREATE CMD-BUF CMD-BUF-SIZE ALLOT 

\ largeur du champ d'impression
\ peut-être modifié par 'PRINT #n' 
VARIABLE FIELD-WIDTH 
6 FIELD-WIDTH !

\ vector permet de creer des variables tableau 1D
: vector CREATE CELLS ALLOT DOES> SWAP CELLS + ;

\ paramètres de la ligne en cours d'analyse 
2 vector src-line
\ champs de src-line
0 CONSTANT LINE-ADDR \ adresse du texte
1 CONSTANT LINE-LEN  \ longueur du texte en caractères

\ initialise src-line
\ paramètres:
\   c-addr  adresse de la ligne
\   u       longueur de la ligne
: line! ( c-addr u -- )
	LINE-LEN src-line ! 
	LINE-ADDR src-line !
;

\ empile l'information de src-line 
\ sortie:
\   c-addr  adresse de la ligne
\   u       longueur de la ligne
: line@ ( -- c-addr u )
	LINE-ADDR src-line @
	LINE-LEN src-line @
;

\ retourne l'adresse de la ligne BASIC
: line-addr ( -- c-addr )
	LINE-ADDR src-line @
;

\ source pour la ligne BASIC à interpréter
\ information mise à jour par l'analyseur
\ lexical
2 vector tb-src 

1 CONSTANT TB-SRC-IN \ adresse dans la chaîne
2 CONSTANT TB-SRC-CNT \ compte caractères restant 

\ sauvegarde c-addr u dans tb-src
: tb-src! ( c-addr u -- )
	TB-SRC-CNT tb-src !
	TB-SRC-IN tb-src ! 
;

\ empile tb-src
\  sortie:
\   c-addr   adresse dans la chaîne 
\   u        longueur restante
: tb-src@ ( -- c-addr u )
	TB-SRC-IN tb-src @ 
	TB-SRC-CNT tb-src @
;

\ saute les espaces entre les lexèmes
\ met à jour tb-src

: skip-spaces ( -- )
	tb-src@ DUP IFF
		BL SKIP 
		tb-src!
	ELSE 
		2DROP 
	ENDIF 
; 

\ ********************************
\ support de débogage seulement
\ ********************************

\ profondeur de la pile des retours
: rdepth ( -- u )
	."  <R:"
	rp0 @ rp@ - cell / . 
	." > "
;

\ affiche no de ligne en 
\ cours d'interprétation
: disp-line# 
    cr ." line# " 
	line@ drop i@  . .s cr 
;

\ affiche un numéro indiquant 
\ la position de la trace 
\ et le contenu de la pile
: trace ( n -- )
	cr
	[char] ( emit .
	[char] ) emit
	.s rdepth 
;


\ **********************************


\ ************************************************************
\
\     ** Espace mémoire pour le programme tiny BASIC **
\  
\  Un programme est une liste de lignes 
\  interprétée l'une à la suite de l'autre
\  chaque ligne de texte comprend une entête
\
\   champ   | grandeur | description
\ -------------------------------------------------------------
\   #ligne  | 2 octets | numéro de ligne en binaire {1..32767}
\   long.   | 1 octet  | longueur de la ligne inclant l'entête {1..80}
\   texte   | long.-3  | texte source BASIC  
\
\ ************************************************************

\ grandeur du bloc mémoire contenant le programme BASIC
65536 CONSTANT PROG-MEM-SIZE

\ création du bloc devant 
\ contenir le texte du programme BASIC 
CREATE PROG-MEM PROG-MEM-SIZE ALLOT

\ addresse fin du programme
VARIABLE PROG-END 

\ met à 0 la mémoire PROG-MEM
: clear-prog-mem ( -- )
	PROG-MEM PROG-MEM-SIZE 0 FILL
	PROG-MEM PROG-END !
;



\ grandeur entête de ligne source 
\ dans PROG-MEM 
3 CONSTANT HEADER-SIZE

\ position des champs dans l'enregistrement ligne
0 CONSTANT TB-LINE#  \ numéro de ligne en binaire
2 CONSTANT TB-LN-LEN  \ longueur de la ligne incluant l'entête
3 CONSTANT TB-TEXT  \ texte BASIC

\ empile la longueur de la ligne
\ paramètres:
\    addr    adresse de la ligne 
\ sortie:
\    len     longueur de la ligne
: line-len@ ( addr -- len ) 
	TB-LN-LEN + C@ 
;

\ affecte une valeur au champ 
\  TB-LN-LEN
\ paramètres:
\     n+      longueur
\     addr    adresse de la ligne
: line-len! ( n+ addr -- )
	TB-LN-LEN + !
;

\ retourne l'adresse du texte 
\ paramètres:
\    line-addr  adresse de la ligne
\ sortie:
\    text-addr  début du texte 
: text-addr ( line-addr -- text-addr )
	TB-TEXT + 
;

\ retourne la longueur du texte
\ paramètres:
\   addr    adresse de la ligne
\ sortie:
\    len    longueur du texte 
: text-len ( addr -- len ) 
	TB-LN-LEN + c@ HEADER-SIZE - 
;


\ initialiase tb-sr à 
\ partir de l'infomration
\ contenue dans src-line 
: tb-src-init ( -- )
	line-addr DUP 
	text-addr 
	SWAP text-len 
	tb-src!
;

\ retourne vrai si 
\ addr < PROG-END 
\ paramètres:
\   addr  adresse à vérifier
\ sortie:
\   flag  vrai si addr < PROG-END @
: not-at-end ( addr -- flag ) 
	PROG-END @ <
;

\ saute à la ligne BASIC suivante
\ paramètres:
\   addr   adresse de la ligne actuelle
\ sortie:
\   addr-next  adresse de la ligne suivante
: skip-to-next-line ( addr -- addr-next )
	DUP TB-LN-LEN + C@ +
;

\ recherche une ligne BASIC 
\ input:
\    n   numéro ligne recherchée
\  output:
\    addr   où la recherche s'est interrompue
\    flag   vrai si n==line1 à addr 
: search-line ( n -- addr flag )
	PROG-MEM
	TRUE ( boucle tant que vrai) 
	BEGIN ( n addr loop-flag )
		OVER not-at-end AND WHILE
		2DUP i@ SWAP < IFF  
			skip-to-next-line 
			TRUE ( n addr TRUE )
		ELSE 
			FALSE ( recherche terminée, n<=line# )
		ENDIF 
	REPEAT ( n addr )
	SWAP OVER I@ = 
; 


\ supprime la ligne 
\ paramètres:
\    addr  addresse de la ligne 
: del-line ( addr -- )
	DUP TB-LN-LEN + C@ ( longueur de la ligne ) 
	DUP >R
	OVER + SWAP ( src dest ) 
	PROG-END @ OVER - ( src dest cnt )
	CMOVE 
	PROG-END @ R> - PROG-END !  
;

\ vérifie s'il y a suffisamment 
\ d'espace dans PROG-MEM
\ pour insérer la nouvelle ligne.	 
: space? ( addr cnt -- ) 
	+ PROG-MEM PROG-MEM-SIZE +
	>= IFF ERR-MEM-FULL THROW ENDIF
;

\ création d'un espace d'insertion
\ dans PROG-MEM si le point d'insertion
\ est avant la fin du programme.
\ paramètres:
\    pos  	point d'insertion 
\    size 	grandeur 
: gap ( pos size -- ) 
	OVER PROG-END @ < IFF
		>R 
		DUP R@ + ( src dest )
		OVER PROG-END @ SWAP - ( src dest cnt )
		2DUP space?
		CMOVE> 
		PROG-END @ R> + PROG-END !
	ELSE 
		PROG-END @ + PROG-END ! 
		DROP
	ENDIF 
;

\ enregistre le numéro de ligne
\ paramètres:
\ 	llen	longueur de la ligne
\   n		numéro de ligne
\   addr    address entête ligne 
: line-header! ( llen n addr  -- addr-tb-text )
	DUP -ROT I!
	TB-LN-LEN + DUP -ROT C!
	1+ ( champ TB-TEXT )
;

\ insère une ligne de code dans PROG-MEM 
\ paramètres:
\   n   	numéro de la ligne à insérer 
\   addr	point d'insertion  
: insert-line ( n addr  -- )
	DUP tb-src@ 2>R  R@ HEADER-SIZE + DUP >R  gap  
	R> -ROT line-header! ( champ TB-LINE# ) 
	2R> ROT SWAP CMOVE ( copie du texte, champ TB-TEXT ) 
; 

\ Ajoute la ligne pointée par tb-src à PROG-MEM 
\ paramètres:
\	n		numéro de la ligne 
: add-line ( n  -- )
	DUP search-line  \ est-ce que ce no de line existe ?
	IFF ( ligne existante ) 
		DUP del-line ( supprime l'existante ) 
	ENDIF 
	tb-src@ SWAP DROP 
	IFF insert-line ( n addr -- ) ENDIF 
;

\ *********************************************************

\ vecteur permettant de 
\ sauvegarder le contexte
\ voir command STOP
4 vector stop-context 
\ champ de stop-context 
0 CONSTANT STOP-SRC-ADDR 
1 CONSTANT STOP-SRC-LEN
2 CONSTANT STOP-TB-IN 
3 CONSTANT STOP-TB-CNT

\ la commande STOP 
\ sauvegarde le contexte 
\ de l'interpréteur 
: save-stop-context ( -- ) 
	line@ ( c-addr len )
	STOP-SRC-LEN stop-context !
	STOP-SRC-ADDR stop-context !
	tb-src@ ( src-in src-cnt )
	STOP-TB-CNT stop-context !
	STOP-TB-IN stop-context !
;

\ la commande RUN 
\ doit restaurer le contexte
\ sauvegardé par STOP
: restore-stop-context ( -- )
	STOP-SRC-ADDR stop-context @
	STOP-SRC-LEN stop-context @
	line!
	STOP-TB-IN stop-context @
	STOP-TB-CNT stop-context @
	tb-src!
;

\ si vrai le dernier lexème retiré par 'next-lex' 
\ n'a pas été utilisé 
\ et a été sauvegardé dans 'ungot-lex'
VARIABLE saved-lex
FALSE saved-lex ! 

\ lexème remis en banque
3 vector ungot-lex 
\ champ pour accéder à ungot-lex 
0 CONSTANT UNGOT-ADDR \ pointeur chaine 
1 CONSTANT UNGOT-CNT \ longueur chaine 
2 CONSTANT UNGOT-ID \ identifiant lexème 

\ sauvegarde le lexème inutilisé.
: ungot! ( addr cnt idlex -- )
	?DUP IFF 
		UNGOT-ID ungot-lex ! 
		UNGOT-CNT ungot-lex !
		UNGOT-ADDR ungot-lex ! 
		TRUE saved-lex !
	ELSE
		2DROP
		FALSE saved-lex !
	ENDIF 
;

\ récupère le lexème sauvegardé
: ungot@ ( -- addr cnt idlex )
    saved-lex @ IFF 
		UNGOT-ADDR ungot-lex @ 
		UNGOT-CNT ungot-lex @
		UNGOT-ID ungot-lex @ 
		FALSE saved-lex !
	ELSE 
		0
	ENDIF 
;

\ mot pour créer des table d'entiers 16 bits
\ usage: INT16-ARRAY ( n -- ) nom-table
: INT16-ARRAY CREATE INTGR-SIZE * ALLOT ( 'cccc' )  
	DOES> ( u addr -- addr+2*n ) SWAP INTGR-SIZE * + ;



\ profondeur d'imbrication pile de contrôle
variable cstack-ptr 
-1 cstack-ptr !  \ pile vide

\ mot pour créer la pile de contrôle utilisée 
\ par les boucles 'FOR ... NEXT'
\ et les GOSUB | RETURN
: CSTACK-ARRAY  CREATE 4 * CELLS ALLOT 
	DOES> ( u addr -- addr+4*n*8 ) cstack-ptr @ 4 * ROT + CELLS + ;
  
\ pile de contrôle, 16 niveaux.
16 CSTACK-ARRAY cstack 
\ champs d'une cellule cstack pour les FOR...NEXT
0 CONSTANT FOR-LIMIT 
1 CONSTANT FOR-STEP 
2 CONSTANT LOOP-IN 
3 CONSTANT LOOP-CNT 
\ champs d'une cellule cstack pour les GOSUB... RETURN
0 CONSTANT RET-LN-ADDR 
1 CONSTANT RET-LN-LEN 
2 CONSTANT RET-SRC-IN
3 CONSTANT RET-SRC-CNT 

\ appellé par GOSUB pour 
\ sauvegarder le contexte 
\ de retour de sous-routine
: push-branch-context ( -- ) 
	1 cstack-ptr +!
	line@ RET-LN-LEN cstack !
	RET-LN-ADDR cstack !
	tb-src@
	RET-SRC-CNT cstack !
	RET-SRC-IN cstack !
;

\ appellé par RETURN pour
\ retourné au point d'appel 
\ de la sous-routine
: pop-branch-context ( -- )
	RET-LN-ADDR cstack @
	RET-LN-LEN cstack @   
	line!
	RET-SRC-IN cstack @
	RET-SRC-CNT cstack @  
	tb-src!
	-1 cstack-ptr +! 
;


\ initialise limite
\ appellé par TO 
: limit! ( limit -- )
	FOR-LIMIT cstack !
;

\ appellé par NEXT
: limit@ ( -- limit )
	FOR-LIMIT cstack @
;

\ initilialise la limite
\ appellé  par TO et STEP
: step! ( step -- )
	FOR-STEP cstack ! 
;

\ empile l'incrément
\ appellé par NEXT 
: step@ ( -- step )
	FOR-STEP cstack @ 
;

\ initialise le point de branchement
\ pour NEXT 
\ appellé par TO et STEP
: loop-back-save ( -- )
	tb-src@ ?DUP
	0= IFF 
		DUP HEADER-SIZE + SWAP 
		TB-LN-LEN + C@ 
		HEADER-SIZE - 
	ENDIF 
	LOOP-CNT cstack !
	LOOP-IN cstack !
;

\ appellé par NEXT 
\ pour boucler au début du FOR..NEXT
: loop-back-restore (  -- )
	LOOP-IN cstack @
	LOOP-CNT cstack @
	2DUP tb-src! line!
;


\ variables booléennes
VARIABLE flags 
0 flags !

\ bits des drapeaux
1 CONSTANT FRUN   ( 1<<0)
2 CONSTANT FSTOP  ( 1<<1)
4 CONSTANT FLINE-DONE ( 1<<2)

\ lève le drapeau
: set-flag ( drapeau -- )
	flags @ OR  
	flags !
;

\ baisse le drapeau
: clear-flag ( drapeau -- )
  INVERT flags @ AND  
  flags !
; 

\ test l'état du drapeau
: test-flag ( drapeau -- FALSE|TRUE )
   flags @ AND 0 <> 
;


\ tableau des variables tiny BASIC {A..Z} 
26 	INT16-ARRAY tb-vars 

\ tableau '@' 
\ taille 256 entiers
256 INT16-ARRAY at-array 

\ vide la pile des arguments
: preset ( i*x -- )
	sp0 @ sp!
;

\ affiche la ligne BASIC
\ entrée:
\    addr   addresse de la ligne
: list-line ( addr -- )
	CR DUP I@ 5 .R SPACE 
	TB-LN-LEN + DUP C@ HEADER-SIZE - SWAP 
	1+ SWAP TYPE 
;


\ affiche le numoro de la ligne
\ rapporte les erreurs 
\ et vide la pile des arguments
\ paramètres:
\   n           code d'erreur
: error ( n -- )
	?DUP IFF
		CR ." erreur: " DUP >R . CR 
		line@
		FRUN test-flag IFF
			OVER list-line
			CR 6 SPACES
			HEADER-SIZE - 
		ELSE
			2DUP TYPE CR
		ENDIF
		SWAP DROP  
		tb-src@ SWAP DROP - 1- 
		SPACES [CHAR] ^ EMIT CR  R>
		CASE
			ERR-SYNTAX OF ." Erreur de syntaxe." ENDOF 
			ERR-UNKOWN OF ." Commande inconnue." ENDOF
			ERR-CMD-LINE-ONLY OF ." Ne peut-être utilisé que sur la ligne de commande." ENDOF
			ERR-RT-ONLY OF ." Ne peut-être utilisé que dans un programme." ENDOF
			ERR-MISSING OF ." Argument manquant." ENDOF
			ERR-NOT-LINE OF ." Aucune ligne ne porte ce numéro." ENDOF
			ERR-MEM-FULL OF ." mémoire insuffisante." ENDOF 
			ERR-DIV0 OF ." division par zéro." ENDOF 
			ERR-BAD-ADDR OF ." adresse invalide." ENDOF 
			ERR-NOT-A-FILE OF ." Fichier inexistant." ENDOF 
			ERR-USER-BREAK OF ." Programme interrompu par l'utilisateur." ENDOF 
		ENDCASE 
		0 flags !
		-1 cstack-ptr ! 
		preset \ vide la pile 
	ENDIF  
;


\ génère une erreur si 
\ la commande est invoquée
\ sur la ligne de commande
: run-time-only ( -- )
	FRUN test-flag INVERT  IFF 
			ERR-RT-ONLY throw  
	ENDIF 
;

\ génère une erruer si 
\ la commande est invoquée 
\ en run time
: cmd-line-only ( -- )
	FRUN test-flag IFF 
		ERR-CMD-LINE-ONLY throw
	ENDIF
;

\ retourne l'adresse de la variable 
\ dans le tableau tb-vars
\ paramètres:
\   c    caractère nom de la variable
\ sortie:
\   addr    addresse de la variable
: var-addr ( c -- addr )
	TOUPPER 
	[CHAR] A - tb-vars
;

\ retourne le nom de la variable
\ paramètres:
\ 	addr   addresse de la variable
\ sortie:
\	c       nome de la variable {'A'..'Z'}
: var-name ( addr -- c )
	0 tb-vars - 
	 2/
	[CHAR] A +
;

\ retourne la valeur de la variable 
: var@ ( c -- n ) 
	var-addr 
	I@
; 

\ affecte une valeur à la variable 
: var! ( n c -- ) 
	var-addr I!
;

\ retourne la valeur du tableau 
\ à l'index i 
: @@ ( i -- n )
	at-array I@
;

\ affecte une valeur au tableau 
\ à l'indice i 
: @! ( n i -- ) 
	at-array I!
;

\ lit une commande du clavier
\ paramètres:
\   buf    adresse tampon de saisie
\   size   longueur du tampons en octets
\ sortie:
\   buf    adresse du tampon 
\   cnt    nombre de caractères lues 
: read-cmd-line ( buf size -- buf cnt )
	OVER >R 
	ACCEPT CR
	R> SWAP 
;

\ extrait le prochain caractère 
\ de tb-src
\ modifie tb-src
: next-char ( -- c )
	tb-src@
	DUP IFF 
		>R 
		COUNT 
		R> 1- 
		SWAP
	ELSE 
		0 
	ENDIF
	>R tb-src! R>  
; 

\ restitue le dernier caractère retiré.
\ modifie tb-src
\ paramètres:
\	c   dernier carctère retourné par next-char
: unget-char ( c --  )
	IFF 
		tb-src@
		1+ SWAP 1- SWAP
		tb-src!
	ENDIF
; 

\ retourne vrai si 
\ c est une lettre
\ paramètres:
\    c    caractère à vérifié
\ sortie:
\    f     VRAI|FAUX
: alpha? ( c --  f )
	TOUPPER DUP
	[CHAR] A >= 
	SWAP [CHAR] Z <=
	AND 
; 

\ retourne vrai si 
\ c est un chiffre decimal
\ paramètres:
\    c    caractère à vérifié
\ sortie:
\    f     VRAI|FAUX
: digit? ( c -- f )
	DUP [CHAR] 0 >=
	SWAP [CHAR] 9 <= 
	AND  
;

\ autres charactères acceptés 
\ dans les noms {'.','?','_'}
\ paramètres:
\    c    caractère à vérifié
\ sortie:
\    f     VRAI|FAUX
: other-valid? ( c-- f )
	DUP [CHAR] . = SWAP 
	DUP [CHAR] ? = SWAP 
	[CHAR] _ = 
	OR OR
;

\ retourne vrai si le caractère 
\ est accepté dans un nom de commande
\ paramètres:
\    c    caractère à vérifié
\ sortie:
\    f     VRAI|FAUX
: name-char? ( c -- f )
	DUP alpha? SWAP DUP digit? 
	SWAP other-valid? OR OR 
;

\ convertie la chaîne en majuscules
\ in situ.
\ paramètres:
\   c-addr u   chaîne à convertir
\ sortie:
\   c-addr u   chaîne en majuscules
: UPPER ( c-addr u -- c-addr u ) 
	2DUP 
	BEGIN 
		DUP WHILE 
		SWAP DUP
		C@ TOUPPER
		OVER C!
		1+ SWAP 1-
    REPEAT
    2DROP		 
; 

defer valid-char?

\ avance le pointeur de texte 
\ jusqu'au prochain caractère non valide 
\ paramètres:
\ 	tb-src  texte à analyser 
\           modifié par next-char|unget-char
\ sorties:
\ 	c-addr  position après le lexème 
: scan-lexem ( -- c-addr )
	BEGIN 
		next-char 
		DUP valid-char?
	WHILE
		DROP
	REPEAT
	unget-char
	tb-src@ DROP 
;

\ extrait un nom de la source
: scan-name ( -- c-addr u ) 
	['] name-char? is valid-char? 
	scan-lexem 
; 

\ extrait un entier de la source
\ sorties:
\ 	c-addr u  état de tb-src après le scan 
: scan-integer ( -- c-addr u )
	['] digit? is valid-char?
	scan-lexem
;

\ avance tb-src après le caractère c
: skip-after ( c -- ) 
	>R 
	tb-src@ R> SCAN 
	1- SWAP 1+ SWAP
	tb-src! 
;

\ extrait les autres types d'unité lexicales 
\ paramètres:
\	c        premier caractère du lexème
\ sortie: 
\	IDLEX-*   identifiant type lexème 
: scan-other ( c -- idlex )
	CASE
	[CHAR] * OF IDLEX-MUL ENDOF 
	[CHAR] / OF IDLEX-DIV ENDOF 
	[CHAR] % OF IDLEX-MOD ENDOF
	[CHAR] + OF IDLEX-ADD ENDOF 
	[CHAR] - OF IDLEX-SUB ENDOF
	[CHAR] = OF IDLEX-EQU ENDOF 
	[CHAR] > OF
		next-char DUP  
		[CHAR] = = IFF
				DROP IDLEX-GE \ >= 
			ELSE 
				unget-char
				IDLEX-GT \ >
			ENDIF
	ENDOF 
	[CHAR] < OF
		next-char DUP
		[CHAR] = = IFF
				DROP IDLEX-LE \ <=
			ELSE DUP [CHAR] > = IFF 
					DROP IDLEX-NE \ <>
				ELSE
					unget-char
					IDLEX-LT \ <
				ENDIF 
			ENDIF 
	ENDOF
	[CHAR] \ OF IDLEX-BKSLASH ENDOF 
	[CHAR] ? OF IDLEX-LABEL ENDOF 
	[CHAR] @ OF IDLEX-ARRAY ENDOF	
	[CHAR] # OF IDLEX-SHARP ENDOF
	[CHAR] ( OF IDLEX-LPAREN ENDOF
	[CHAR] ) OF IDLEX-RPAREN ENDOF
	[CHAR] , OF IDLEX-COMMA ENDOF
	[CHAR] ; OF IDLEX-SCOL ENDOF
	[CHAR] " OF 
		[CHAR] " skip-after 
		IDLEX-QUOTE 
		ENDOF 
	[CHAR] ' OF
		[CHAR] ' skip-after
		IDLEX-QUOTE
		ENDOF 
	ERR-SYNTAX THROW 
	SWAP ENDCASE
	tb-src@ DROP 
;

\ vérifie s'il s'agit d'un nom de commande ou variable
\ retourne l'adresse de la variable ou le pointeur de 
\ la chaîne comptée.
\ paramètres:
\   c-addr u   chaîne extraite 
\ sortie:
\     c-addr u IDLEX-LABEL | var-addr 0 IDLEX-VAR 
: name-value ( c-addr u  -- c-addr u IDLEX-LABEL | var-addr 0 IDLEX-VAR  )
	DUP 1 = IFF 
		DROP C@  var-addr 
		0 IDLEX-VAR
	ELSE 
		IDLEX-LABEL 
	ENDIF 
;

\ convertie la chaîne en entier 
: atoi16 ( c-addr u -- i16 )
	2>R 0. 2R> >NUMBER 
	2DROP DROP
	sign-extend  
; 

\ convertie la chaîne en entier
\ et ajoute l'identifiant du lexème
: int-value ( c-addr u -- n 0 IDLEX-INTEGER )
	atoi16 0 IDLEX-INTEGER 
;

\ extrait le prochain lexème
\ paramètres:
\  tb-src  chaîne comptée à analyser
\          mis à jour par le scanner
\ sortie:
\  lex-val IDLEX  valeur du lexème et son identifiant.
\  IDLEX-NULL     analyse tb-src complétée.
: next-lex (  --  [lex-val IDLEX | 0 0 IDLEX-NULL] )   
	ungot@ ?DUP 0= IFF 
		skip-spaces tb-src@ ( -- addr u )
		DUP 0> IFF  
			DROP >R  \ R: adresse début lexème
			next-char DUP alpha? IFF 
				DROP scan-name R@ - R> SWAP ( -- c-addr len )
				name-value
			ELSE 
				DUP digit? IFF
					DROP scan-integer R@ - R> SWAP ( -- c-addr len )
					int-value
				ELSE
					scan-other 
					R@ - r> SWAP ( -- IDLEX c-addr cnt )
					ROT 
					DUP IDLEX-QUOTE = IFF 
						-ROT
						2 - swap 1+ swap
						ROT
					ENDIF
				ENDIF	 
			ENDIF
		ELSE
			SWAP DROP 0 IDLEX-NULL FLINE-DONE set-flag
		ENDIF 
	ENDIF
; 

\ vérifie si le lexème suivant
\ correspond à celui attendue 
\ termine avec erreur si ce n'est pas le cas
\ paramètres:
\    idlex   identifiant lexème attendue
\ sortie:
\   val-lo val-hi idlex  lexème accepté
: expect ( idlex -- val-lo val-hi idlex )
	>R  
	next-lex R@
	<> IFF
		R> DROP 
		ERR-SYNTAX THROW 
	ELSE 
		R>
	ENDIF  
; 

\ le prochain lexème 
\ doit-être '('
\ termine avec erreur 
\ si ce n'est pas le cas.
: expect-( ( -- )
	IDLEX-LPAREN expect
	DROP 2DROP
;

\ le prochain lexème
\ doit-être ')'
\ termine avec erreur 
\ si ce n'est pas le cas.
: expect-) 
	IDLEX-RPAREN expect
	DROP 2DROP
;

defer relation

\ évaluation d'un facteur
\ factor ::= ['-'|'+']integer |
\            variable | function | 
\            '(' expression ')' 
: factor (  --  n ) 
	next-lex 
	case 
	IDLEX-VAR OF DROP I@  ENDOF 
	IDLEX-INTEGER OF DROP ENDOF
	IDLEX-ARRAY OF
		2DROP 
		relation
		at-array i@ 
	ENDOF
	IDLEX-LABEL OF
		find IFF
			execute
		ELSE
			ERR-UNKOWN THROW
		ENDIF 
	ENDOF
	IDLEX-LPAREN OF
		2DROP relation
		expect-)
	ENDOF
	SWAP
	ENDCASE 
	sign-extend
;

\ premier facteur du terme
: factor-left (  -- n ) 
	next-lex 
	DUP IDLEX-ADD = OVER IDLEX-SUB = OR IFF 
		>R 2DROP 0 >R
		factor 
		R> SWAP 
		R>  
		IDLEX-SUB = IFF - ELSE + ENDIF
	ELSE
		ungot!
		factor
	ENDIF 
;

\ évaluation d'un terme 
\  term ::= factor ['*'|'/'|'%' factor]* 
: term (  --  n )
	factor-left >R
	BEGIN
		next-lex 
		DUP IDLEX-MUL >= OVER IDLEX-MOD <= AND WHILE 
		>R 2DROP  
		factor 
		2r> >r SWAP R> 
		CASE 
		IDLEX-MUL OF * ENDOF 
		IDLEX-DIV OF / ENDOF 
		IDLEX-MOD OF MOD ENDOF
		SWAP 
		ENDCASE
		>R 
	REPEAT
	ungot!
	R> 
; 

\ évaluation d'une expression
\ expression ::= term ['-'|'+' term]*
: expression  (  --  n )
	term 
	>r
	BEGIN
		next-lex 
		dup IDLEX-ADD = OVER IDLEX-SUB = OR WHILE
		>R 2DROP
		term
		2R> >R SWAP R>
		IDLEX-ADD = IFF 
			+
		ELSE
			-
		ENDIF
		>R
	REPEAT 
	ungot!
	R>
;

\ définition diférée de 'relation'
\ évaluation d'une relation de comparaison
\ forme d'une relation:  expr [ op-rel expr ]
\ op-rel -> { '=','>','>=','<','<=','<>' }  
: rel (  -- n )
	expression
	>R 
	next-lex 
	DUP IDLEX-EQU >= OVER IDLEX-NE <= AND IFF 
		>R 2DROP
		expression 
		2R> SWAP -ROT 
		CASE 
		IDLEX-EQU OF = ENDOF 
		IDLEX-LT OF < ENDOF 
		IDLEX-LE OF <= ENDOF
		IDLEX-GT OF > ENDOF 
		IDLEX-GE OF >= ENDOF 
		IDLEX-NE OF <> ENDOF
		SWAP 
		ENDCASE
	ELSE  
		ungot!
		R> sign-extend
	ENDIF
; 

' rel is relation 

\ BASIC: LET @(expr)=relation
\ affecte une valeur 
\ au tableau @
: let-array (  -- )
	IDLEX-LPAREN expect DROP 2DROP
	relation
	IDLEX-RPAREN expect  DROP 2DROP 
	at-array 
	IDLEX-EQU expect
	DROP 2DROP
	relation 
	SWAP I!
; 

\ BASIC: LET var=relation
\ affecte une valeur 
\ à une variable A..Z 
: let-var ( var-addr -- )
	IDLEX-EQU expect
	DROP 2DROP 
	relation 
	$FFFF AND SWAP I! 
; 

\ passe à la ligne suivante
: next-line  ( -- )
	line@ + DUP PROG-END @ >= IFF 
		FRUN clear-flag 
	ENDIF 
	DUP 2 + c@ line!
	tb-src-init
; 

\ execute la ligne tiny-basic
: line-eval (  -- )
	FLINE-DONE clear-flag 
	BEGIN
	FLINE-DONE test-flag 0= 
	WHILE 
		next-lex
		CASE 
		IDLEX-NULL OF 2DROP FLINE-DONE set-flag ENDOF
		IDLEX-LABEL OF 
			find \ doit-être un nom de cmd 
			IFF 
				execute
			ELSE 
				ERR-UNKOWN THROW  
			ENDIF
		ENDOF 
		IDLEX-SCOL OF 2DROP ENDOF 
		ERR-SYNTAX THROW
		ENDCASE 
	REPEAT 
;


\ saisie d'une expression et
\ sauvegarde sa valeur dans
\ la variable
\ paramètres:
\ 	var-addr   addresse de la  variable
: user-input ( var-addr -- )
	\ sauvegarde du contexte
	line@ 2>R
	tb-src@ 2>R
	HERE CMD-BUF-SIZE ALLOT \ alloue un tampon de saisie
	CMD-BUF-SIZE read-cmd-line 
	2DUP line! 
	tb-src!
	next-char 
	DUP alpha? IFF 
		TOUPPER
	ELSE 
		unget-char
		rel
	ENDIF 
	SWAP I! 
	\ restauration du contexte 
	2R>
	tb-src! 
	2R> 
	line!
	CMD-BUF-SIZE NEGATE ALLOT \ libère le tampon de saisie
;

\ analyse le texte pour obtenir
\ la ligne cible d'un GOTO ou GOSUB
: parse-target ( -- addr u )
	next-lex IDLEX-INTEGER = IFF 
		DROP search-line IFF  ( addr )
			DUP TB-LN-LEN + C@  ( addr u )
		ELSE 
			ERR-NOT-LINE THROW 
		ENDIF 
	ELSE
		ERR-SYNTAX THROW 
	 ENDIF
;

\ descripteur de fichiers 
0 value fd-in  \ fichier ouvert en lecture
0 value fd-out \ fichier ouvert en écriture

\ *************************
\  fonctions tiny BASIC
\ *************************

\ BASIC: ABS(expr)
\ retourne la valeur 
\ absolue d'expr  
: ABS (  -- u )
	expect-(
	expression
	expect-) 
	ABSS 
; 

\ BASIC: ASC(letter)
\ retourne le code ASCII 
\ d'un caractère
: ASC ( -- n )
	expect-(
	next-char
	expect-)
;


\ BASIC: KEY var
\ attend que l'utilisateur
\ enfonce une touche et 
\ sauvegarde sa valeur dans
\ la variable
: KEY ( -- c )
	IDLEX-VAR expect 
	2drop 
	KEY SWAP I!
;

\ BASIC: KEY?
\ retourne vrai si une touche
\ a été enfoncée
: KEY? ( -- f )
	KEY?
;

\ BASIC: MSEC
\ temps système en millisecondes
\ la valeur retournée est modulo 65536
: MSEC ( -- msec )
	utime drop 1000 /
	$FFFF AND 
;

\ BASIC: NOT relation
\ négation logique d'une relation
: NOT ( -- f )
	relation 
	0= 
;

\ BASIC: RND(expr) 
\ retourne un entier aléatoire 
\ dans l'interval {1..expr}
: RND ( -- u )
	expect-( 
	expression 
	expect-)
	ABSS RANDOM 1+
; 

\ *********************
\ commandes tiny BASIC 
\ *********************

\ BASIC: BEEP
\ emit un son court 
: BEEP 
	7 EMIT
	125 MS 
;

\ BASIC: CLS
\ efface l'écran du terminal
: CLS ( -- )
	page 
;

\ BASIC: DIR 
\ affiche la liste de programmes BASIC
: DIR
	S" ls | grep [.][bB][aA][sS] --color=never" 
	system
;


\ BASIC: END 
\ termine l'exécution du programme
: END ( -- )
	run-time-only
	tb-src@ + 0
	FRUN clear-flag 
;

\ déclaration pour la définition 
\ différée de la commande LET
defer LET 

\ BASIC: FOR var=expr TO expr [STEP expr]
\ initialisation boucle FOR..NEXT 
\ syntaxe: FOR var=expr 
: FOR ( -- )
	LET \ initialise la variable de contrôle
	1 cstack-ptr +! \ incrémente la profondeur de boucle
; 

\ BASIC: GOSUB n 
\ appel de sous-routine 
\ 'n' est le numéro de ligne
\ où débute la sous-routine
: GOSUB ( -- )
	run-time-only 
	parse-target 
	push-branch-context 
	line! 
	tb-src-init
; 

\ BASIC: GOTO n
\ saut inconditionnel 
\ 'n'est le numéro de 
\ ligne où doit continuer
\ l'exécution du programme.
: GOTO ( -- )
	run-time-only
	parse-target
	line! 
	tb-src-init 
; 

\ Affiche  l'information sur le programme.
: HI ( -- )
    CR
    ." -------------------------------------" CR 
	." Tiny BASIC V1.0" CR
	." Copyright Jacques Deschenes, 2022" CR
;

\ BASIC: IF relation THEN liste-commandes
\ exécution conditionnel
\ des commandes qui suivent 
\ l'expression si celle-ci 
\ est vrai 
\ sortie:
\     n    laissé sur la pile pour THEN
: IF ( -- n )
	relation 
;

\ BASIC: INPUT [chaîne]var [,[chaîne]var]*
\ saisie d'une valeur par l'utilisateur
: INPUT ( -- c-addr )
	TRUE
	BEGIN WHILE
		next-lex DUP
		CASE 
		IDLEX-QUOTE OF
			DROP TYPE
			IDLEX-VAR expect
			2DROP  user-input
			TRUE 
		ENDOF 
		IDLEX-VAR OF
			2DROP DUP
			var-name EMIT [CHAR] : EMIT 
			user-input
			TRUE 
		ENDOF
		IDLEX-COMMA OF DROP 2DROP TRUE ENDOF
		ungot! FALSE 
		SWAP 
		ENDCASE
	REPEAT
; 

\ définition différée de LET
\ BASIC: LET var=expr [, var=expr]*
\ affectation d'une variable 
: _LET ( -- )
	TRUE \ loop flag exit when FALSE
	BEGIN
	WHILE 
		next-lex
		DUP IDLEX-ARRAY = IFF 
			DROP 2DROP 
			let-array 
		ELSE 
			IDLEX-VAR = IFF 
				DROP let-var 
			ELSE
				ERR-SYNTAX THROW
			ENDIF 
		ENDIF
		next-lex 
		DUP IDLEX-COMMA = IFF 
			DROP 2DROP TRUE 
		ELSE
		    ungot! FALSE 
		ENDIF
	REPEAT 
;

' _LET is LET 

\ BASIC: LIST [n]
\ listing du programme 
\ 'n' est un numéro de ligne optionnel
\ à partir duquel commence le listing
: LIST ( -- )
	cmd-line-only 
	next-lex DUP IDLEX-INTEGER = IFF
		2DROP ( -- start-line# ) 
		>R ( le listing débute à ce numéro )
	ELSE
		ungot!
		1 >R  ( liste à partir du début )
	ENDIF
	PROG-MEM 
	BEGIN
		DUP PROG-END @ < WHILE 
		DUP I@ R@  >= IFF
			DUP list-line 
		ENDIF 
		DUP TB-LN-LEN + C@ +
	REPEAT
	R> 2DROP
;  
 
\ déclaration pour la définition
\ différée de l'interpréteur Tiny BASIC
defer tb-eval

\ déclaration pour la définition différée
\ de la commande BASIC 'NEW' 
defer NEW 

\ BASIC: LOAD nom-fichier
\ charge un fichier programme 
\ en mémoire.
: LOAD (  ) 
	cmd-line-only
	next-lex
	IDLEX-LABEL = IFF 
		NEW 2DUP
		r/o open-file throw TOO fd-in
		." chargement de " type cr
		BEGIN
			CMD-BUF CMD-BUF-SIZE fd-in read-line THROW WHILE 
			CMD-BUF SWAP 
			tb-eval 	
		REPEAT
		DROP
		fd-in close-file THROW
		tb-src@ drop 0 tb-src! 
	ENDIF 
; 

\ définition différée de NEW
\ BASIC: NEW 
\ efface le programme em mémoire
: _NEW 
	cmd-line-only
	clear-prog-mem
	0 flags !
;

' _NEW is NEW 

\ BASIC: NEXT var 
\ contrôle de boucle FOR..NEXT
\ incrémente la variable 
\ et compare sa valeur
\ à la limite
: NEXT ( -- )  
	next-lex IDLEX-VAR <> IFF ERR-SYNTAX THROW ENDIF
	DROP 
	DUP I@ step@ DUP >R
	+ DUP ROT i!
	limit@ R> 0> IFF  
		> IFF  
			-1 cstack-ptr +!
		ELSE 
			loop-back-restore 
		ENDIF
	ELSE
		< IFF 
			-1 cstack-ptr +!
		ELSE 
			loop-back-restore
		ENDIF 
	ENDIF
; 

\ BASIC: PAUSE expr 
\ suspend l'exécution pour 
\ 'expr' millisecondes
: PAUSE ( -- )
	expression 
	ABSS MS
;

: print-relation ( lexème -- ) 
	ungot!
	relation 
	field-width @ .R  
;

\ BASIC: PRINT exp|quote [,expr|quote]*
\ imprime à l'écran
\ syntaxe: [[#n,]expr|"chaine" [,expr|"chaine"]*] 
\ sans arguments envoie un CR+LF 
\ si la liste d'arguments se termine par 
\ une virgule n'envoie pas de CR+LF
: PRINT (  -- )
	 -1 DUP >R 
	BEGIN 
	WHILE 
		next-lex ( -- c-addr u idlex )
		DUP IDLEX-SCOL <> OVER 0<> AND IFF
			R> DROP -1 >R \ active CR en fin de commande 
		 	DUP
		 	CASE
			IDLEX-INTEGER OF 
				print-relation
			ENDOF
			IDLEX-VAR OF
				print-relation 
			ENDOF
			IDLEX-ARRAY OF
				print-relation
			ENDOF 
			IDLEX-SUB OF
				print-relation
			ENDOF 
			IDLEX-ADD OF
				print-relation
			ENDOF 
			IDLEX-BKSLASH OF  \ envoie le code ASCII 0..127
				DROP 2DROP  
				next-lex 
				CASE 
				IDLEX-INTEGER OF 
					DROP 127 AND emit
				ENDOF 
				IDLEX-VAR OF 
					DROP I@ 127 AND EMIT 
				ENDOF 
					ERR-SYNTAX THROW
				ENDCASE
			ENDOF 
			IDLEX-LABEL OF 
				print-relation 
			ENDOF
			IDLEX-LPAREN OF 
				print-relation 
			ENDOF 
			IDLEX-COMMA OF 
				DROP 2DROP R> DROP 0 >R \ désactive CR en fin de commande
			ENDOF 
			IDLEX-SHARP OF
				DROP 2DROP 
				next-lex 
				IDLEX-INTEGER = IFF
					DROP field-width ! 
				ELSE
					ERR-SYNTAX THROW 
				ENDIF
			ENDOF
			IDLEX-QUOTE OF DROP 
				field-width @ OVER - SPACES  
				TYPE
			ENDOF  
			ERR-SYNTAX THROW 
			ENDCASE
			-1
		ELSE 
			DROP 2DROP 0  
		ENDIF 
	REPEAT 
    R> IFF CR ENDIF
; 

' PRINT ALIAS ?

\ BASIC: QUIT 
\ Quitte l'interpréteur BASIC
\ retourne à la ligne de commande 
\ de gforth
: QUIT ( -- )
	ERR-QUIT THROW 
;

\ BASIC: REM texte
\ commentaire ignoré par l'interpréteur
: REM (  --  )
	tb-src@ 
	drop 0 tb-src! 
; 

\ BASIC: RETURN
\ sortie de sous-routine  
: RETURN 
	run-time-only
	pop-branch-context
; 

\ evaluation ligne par ligne 
\ du programme en mémoire
: run-loop
	BEGIN 
		FRUN test-flag WHILE  \ disp-line# rdepth ( debug support )
		line-eval \ 2 trace 
		FSTOP test-flag INVERT IFF next-line ENDIF 
	REPEAT 
;

\ BASIC: RUN [nom-fichier]
\ démarre l'exécution du programme
\ si un nom de fichier est donné en
\ paramètre, le programme est chargé
\ pour exécution immédiate.
: RUN ( -- )
	cmd-line-only
	FSTOP test-flag IFF
		FRUN flags !
		restore-stop-context
		run-loop 
	ELSE 
		LOAD 
		PROG-MEM DUP PROG-END @ < IFF 
			DUP 2 + c@
			line!
			tb-src-init
			-1 cstack-ptr ! 
			FRUN flags !
			run-loop
		ELSE 
			." Aucun programme en mémoire"
		ENDIF
	ENDIF
; 

\ BASIC: SAVE nom-fichier 
\ sauvegarde le programme 
\ dans un fichier 
: SAVE ( 'cccc' )  
	cmd-line-only
	next-lex IDLEX-LABEL = IFF 
		." sauvegarde de " 2DUP TYPE 
		w/o create-file THROW TOO fd-out
		PROG-MEM 
		BEGIN
			DUP PROG-END @ < WHILE
			DUP I@ 0 <# BL HOLD #S #> fd-out write-file throw
			DUP 2 + C@ OVER 3 + OVER 3 - fd-out write-line throw
			+  
		REPEAT
		fd-out close-file THROW 
	ELSE 
		ERR-SYNTAX THROW 
	ENDIF 
; 

\ BASIC: FOR var=expr TO expr STEP expr
\ incrément de boucle FOR..NEXT 
\ détermine l'incrément de la
\ variable. Optionnel valeur par défaut 1 
: STEP ( -- )
	expression
	step!
    loop-back-save
; 

\ BASIC: STOP
\ arrête l'exécution du programme 
\ RUN relance le programme au point d'arrêt 
: STOP ( -- )
	run-time-only
	FRUN clear-flag
	FSTOP set-flag
	save-stop-context 
	tb-src@ + 0 tb-src!
	CR ." programme arrêté par STOP"
; 

\ BASIC: IF relation THEN liste-commande 
\ saute à la fin de la ligne 
\ si la relation est fausse 
: THEN ( n -- ) 
	0= IFF
		tb-src@ + 0 tb-src!
	ENDIF
;


\ BASIC: FOR var=expr TO expr 
\ limite de boucle FOR..NEXT 
: TO 
   expression
   limit!
   1 step!
   loop-back-save
; 

\ évalue la ligne de commande 
\ si débute par no de ligne 
\ sauvegarde la ligne dans 
\ PROG-MEM 
\ sinon exécute la commande.
: cmd-eval ( buf count --  )
	2DUP line! tb-src! 
	next-lex
	DUP IDLEX-INTEGER = IFF
		2drop skip-spaces
		add-line \ ligne de programme à sauvegarder
	ELSE
		ungot! 
		line-eval \ commande interactive 
	ENDIF
;

' cmd-eval is tb-eval

\ interface de commande tiny BASIC	
: CMD-LINE 
	BEGIN 
		CMD-BUF CMD-BUF-SIZE 
		CR [CHAR] # EMIT 
		read-cmd-line ( buf size -- buf cnt )
		['] cmd-eval CATCH ?DUP IFF 
			DUP ERR-QUIT = IFF preset EXIT ENDIF 
			error
		ENDIF 
	AGAIN 
; 
		
\ lance tiny BASIC 			
: BASIC ( -- )
	HI 
	clear-prog-mem
	utime drop seed !  
	CMD-LINE
	CR ." Sortie de tiny BASIC"
	CR ." pour supprimer tiny BASIC"
	CR ." faite: KILL-BASIC"
	CR CR  
; 

\ supprime tiny-basic de 
\ l'environnement forth
: kill-basic
    get-order swap drop 1- 
    set-order 
	current-org set-current 
	forget-basic
;

\ démarre l'interpréteur
\ Utilisez la commande 'QUIT'
\ pour revenir à gforth
BASIC
   


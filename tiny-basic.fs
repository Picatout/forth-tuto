\ tiny BASIC 


require random.fs

MARKER KILL-BASIC 

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


\ identifiant d'unitées lexicales 
0 CONSTANT IDLEX-NULL	\ fin de texte 
1 CONSTANT IDLEX-LABEL	\ nom de commande ou fonction 
2 CONSTANT IDLEX-VAR	\ {A..Z} 
3 CONSTANT IDLEX-ARRAY	\ @ 
4 CONSTANT IDLEX-INTEGER 
5 CONSTANT IDLEX-MUL	\ *
6 CONSTANT IDLEX-DIV	\ /
7 CONSTANT IDLEX-MOD	\ %
8 CONSTANT IDLEX-ADD	\ + 
9 CONSTANT IDLEX-SUB	\ -
10 CONSTANT IDLEX-EQU 	\ =  
11 CONSTANT IDLEX-LT  	\ <
12 CONSTANT IDLEX-LE	\ <=
13 CONSTANT IDLEX-GT	\ >
14 CONSTANT IDLEX-GE 	\ >=
15 CONSTANT IDLEX-NE	\ <>
16 CONSTANT IDLEX-LPAREN	\ (
17 CONSTANT IDLEX-RPAREN	\ ) 
18 CONSTANT IDLEX-SHARP	\ #
19 CONSTANT IDLEX-COMMA \ , 
20 CONSTANT IDLEX-SCOL \ ; 
21 CONSTANT IDLEX-QUOTE \ "
22 CONSTANT IDLEX-BKSLH \ \

\ grandeur des entiers en octets 
2 CONSTANT INTGR-SIZE

\ grandeur du tampon de saisie 
80 CONSTANT CMD-BUF-SIZE

\ tampon ligne de commande 
CREATE CMD-BUF CMD-BUF-SIZE ALLOT 

\ fin du programme 
VARIABLE TXT-END 

\ largeur du champ d'impression
VARIABLE FIELD-WIDTH 
6 FIELD-WIDTH !

\ si vrai le dernier lexème n'a pas été utilisé 
\ est toujours sur la pile.
VARIABLE saved-lex
FALSE saved-lex ! 

\ vector permet de creer des variables tableau 1D
: vector CREATE CELLS ALLOT DOES> SWAP CELLS + ;

\ paramètres de la ligne en cours d'analyse 
2 vector src-line
\ champs de src-line
0 CONSTANT LINE-ADDR \ adresse du texte
1 CONSTANT LINE-LEN  \ longueur du texte en caractères

\ met à jour l'information de src-line
\ entrées:
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
: line@
	LINE-ADDR src-line @
	LINE-LEN src-line @
;

\ retourne l'adresse de la ligne BASIC
: line-addr 
	LINE-ADDR src-line @
;



\ source pour ligne basic à interpréter
\ information mise à jour par l'analyseur
\ lexical
2 vector tb-src 

1 CONSTANT TB-SRC-IN \ adresse dans la chaîne
2 CONSTANT TB-SRC-CNT \ compte restant 

\ mise à jour de tb-src 
\ entrées:
\  c-addr   adresse de la chaîne 
\  u        longueur de la chaîne
: tb-src-update ( c-addr u -- )
	TB-SRC-CNT tb-src ! 
	TB-SRC-IN tb-src ! 
;

\ retourne l'adresse du texte 
\ entrée:
\    line-addr  adresse de la ligne
\ sortie:
\    text-addr  début du texte 
: text-addr ( line-addr -- text-addr )
	3 + 
;

\ retourne la longueur du texte
\ entrée:
\   addr    adresse de la ligne
\ sortie:
\    len    longueur du texte 
: text-len ( addr -- len ) 
	2 + c@ 3 - 
;


\ initialiase tb-sr à 
\ partir de l'infomration
\ contenue dans src-line 
: tb-src-init ( -- )
	line-addr DUP 
	text-addr 
	SWAP text-len 
	tb-src-update
;


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


: INT16-ARRAY CREATE INTGR-SIZE * ALLOT DOES> SWAP INTGR-SIZE * + ;

: FOR-STACK-ARRAY CREATE 4 * CELLS ALLOT DOES> SWAP 4 * CELLS + ;
  
\ FOR..NEXT stack
8 FOR-STACK-ARRAY for-stack 
\ champs d'une cellule for-stack 
0 CONSTANT FOR-LIMIT 
1 CONSTANT FOR-STEP 
2 CONSTANT LOOP-IN 
3 CONSTANT LOOP-CNT 

variable for-depth 
-1 for-depth !  \ pile vide

\ initialise limite
\ appellé par TO 
: limit! ( limit -- )
	for-depth @ 
	for-stack FOR-LIMIT CELLS + !
;

\ appellé par NEXT
: limit@ ( -- limit )
	for-depth @ 
	for-stack FOR-LIMIT CELLS + @
;

\ initilialise la limite
\ appellé  par TO|STEP
: step! ( step -- )
	for-depth @ 
	for-stack FOR-STEP CELLS + !
;

\ appellé par NEXT 
: step@ ( -- step )
	for-depth @ 
	for-stack FOR-STEP CELLS + @
;

\ initialise le point de branchement
\ pour NEXT 
: loop-back! ( src-in src-cnt -- )
	for-depth @
	for-stack DUP >R
	LOOP-CNT CELLS + !
	R> LOOP-IN CELLS + !
;

\ appellé par NEXT 
\ pour boucler au début du FOR..NEXT
: loop-back (  -- )
	for-depth @
	for-stack DUP >R
	LOOP-IN CELLS + @
	R> LOOP-CNT CELLS + @
	tb-src-update
;

\ variables booléennes
VARIABLE flags 
0 flags !

\ constantes drapeaux
1 CONSTANT FRUN 
2 CONSTANT FSTOP 
4 CONSTANT FLINE-DONE 

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


\ array of tiny BASIC variables A..Z 
26 	INT16-ARRAY tb-vars 

\ grandeur du tampon contenant le programme BASIC
65536 CONSTANT PROG-MEM-SIZE 
CREATE PROG-MEM PROG-MEM-SIZE ALLOT
\ format des lignes BASIC:
\   no de ligne entier 16 bits 
\   longueur ligne incluant le no de ligne 1 octet
\   texte BASIC à interpréter 

\ addresse fin du programme
VARIABLE PROG-END 
PROG-MEM PROG-END !


\ table '@' 
\ taill2 256 entiers
256 INT16-ARRAY at-array 

\ constantes identifiant le type d'erreur
0 CONSTANT ERR-NONE
1 CONSTANT ERR-SYNTAX
2 CONSTANT ERR-UNKOWN
3 CONSTANT ERR-RT-ONLY
4 CONSTANT ERR-CMD-LINE-ONLY
5 CONSTANT ERR-MISSING
6 CONSTANT ERR-NOT-LINE
9 CONSTANT ERR-QUIT 

\ rapporte les erreurs 
\ et vide la pile des arguments
: error ( i*x n -- )
	?DUP IFF 
		CR ." erreur: " DUP . CR 
		line@
		FRUN test-flag IFF
			swap 3 + swap 3 -
		ENDIF 
		tb-src@ SWAP DROP - 
		DUP >R TYPE
		CR R> SPACES [CHAR] ^ EMIT CR
		CASE
			ERR-SYNTAX OF ." Erreur de syntaxe." ENDOF 
			ERR-UNKOWN OF ." Commane inconnue." ENDOF
			ERR-CMD-LINE-ONLY OF ." Ne peut-être utilisé que sur la ligne de commande." ENDOF
			ERR-RT-ONLY OF ." Ne peut-être utilisé que dans un programme." ENDOF
			ERR-MISSING OF ." Argument manquant." ENDOF
			ERR-NOT-LINE OF ." Aucune ligne ne porte ce numéro." ENDOF
			-514 OF ." Fichier inexistant." ENDOF 
			-28 OF ." Programme interrompu par l'utilisateur." ENDOF 
		ENDCASE 
		sp0 @ sp! \ vide la pile 
		0 flags !
		-1 for-depth ! 
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

\ empile un entier de 16 bits 
\ à partir de la mémoire 
\ entrée:
\ 	addr  addresse à lire
\ sortie:
\     n   entier lue
: I@ ( addr -- n )
	DUP C@ 256 * 
	SWAP 1+ C@ +
; 

\ dépose un entier 16 bits en mmémoire 
\ entrées:
\	n		entier à déposer
\	addr	addresse destination
: I! ( n addr -- ) 
	DUP 2 PICK 
	8 RSHIFT SWAP C!
	1+ C!
;

\ retourne l'adresse de la variable 
\ dans le tableau tb-vars
: var-addr ( c -- idx )
	TOUPPER 
	[CHAR] A - tb-vars
;

\ affiche le nom de la variable
\ entrée:
\ 	var-addr   addresse de la variable
\ sortie:
\	c       nome de la variable {'A'..'Z'}
: var-name ( var-addr -- c )
	0 tb-vars SWAP - 
	 2/
	[CHAR] A +
;


\ Si le bit 15 == 1
\ étend le signe de l'entier sur 
\ sur le nombre de bits des entiers
\ du système, i.e 32 ou 64 bits. 
: sign-extend ( u16 -- i16 )
	DUP 32767 > IFF
		-1 $ffff - OR 
	ENDIF 
;

\ retourne la valeur de la variable 
: var@ ( c -- n ) 
	var-addr 
	I@ sign-extend
; 

\ affecte une valeur à la variable 
: var! ( n c -- ) 
	var-addr
	I!
;

\ retourne la valeur du tableau 
\ à l'index i 
: @@ ( i -- n )
	at-array I@ sign-extend 
;

\ affecte une valeur au tableau 
\ à l'indice i 
: @! ( n i -- ) 
	at-array I!
;

\ retourne faux si 
\ addr >= PROG-END 
: not-at-end ( addr -- flag ) 
	PROG-END @ <
;

\ saute les espaces entre les lexèmes
\ dans tb-src
: skip-spaces ( -- src-in src-cnt)
	tb-src@  DUP IFF
		BL SKIP 
		2DUP tb-src-update
	ENDIF 
; 

\ saute à la ligne BASIC suivante
\ entrée:
\   addr   adresse de la ligne actuelle
\ sortie:
\   addr-next  adresse de la ligne suivante
: skip-to-next-line ( addr -- addr-next )
	DUP 2 + C@ +
;

\ lit une commande du clavier
\ entrées:
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

\ recherche une ligne BASIC 
\ input:
\    n   numéro ligne recherchée
\  output:
\    addr   addresse de ligne ou point d'insertion
\      f    trouvé ou pas
: search-line ( n -- n addr f )
	-1 ( loop flag ) PROG-MEM 
	BEGIN ( n f addr )
		DUP not-at-end ROT AND WHILE
		DUP >R 
		I@ OVER < IFF  
			R> skip-to-next-line 
			-1 SWAP  ( n -1 addr )
		ELSE 
			R> 0 SWAP ( n 0 addr )
		ENDIF 
	REPEAT
	DUP not-at-end IFF 
		DUP I@  ( n addr ln# )
		2 PICK = 
	ELSE
		FALSE
	ENDIF
; 

\ supprime la ligne 
\ entrée:
\    addr  addresse de la ligne 
: del-line ( addr -- )
	DUP 2 + C@ ( longueur de la ligne ) 
	DUP >R
	OVER + SWAP ( src dest ) 
	PROG-END @ OVER - ( src dest cnt )
	CMOVE 
	PROG-END @ R> - PROG-END !  
;

\ vérifie s'il y a suffisamment 
\ d'espace dans PROG-MEM	 
: space? ( addr cnt -- ) 
	+ PROG-MEM PROG-MEM-SIZE +
	>= IFF ABORT" mémoire insuffisante" ENDIF
;

\ création d'un espace d'insertion
\ dans PROG-MEM 
\ input:
\    pos  point d'insertion 
\    size grandeur 
: gap ( pos size -- ) 
	>R 
	DUP R@ + ( src dest )
	OVER PROG-END @ SWAP - ( src dest cnt )
	2DUP space?
	CMOVE> 
	PROG-END @ R> + PROG-END ! 
;


\ insère une ligne de code dans PROG-MEM 
\ input:
\   tb-src   ligne à insérée 
\   addr	 point d'insertion  
: insert-line ( no-line addr ln-addr ln-cnt -- )  
	2>R DUP PROG-END @ < IFF
		DUP R@ 3 + gap ( no-ligne addr addr size -- no-ligne addr )
	ELSE
		PROG-END @
		R@ + 3 + PROG-END !
	ENDIF
	SWAP OVER I!
	2 + R@ 3 + OVER C! 
	1+ 2R> ROT SWAP CMOVE 
; 

\ copie src-line dans PROG-MEM 
\ le numéro de ligne est recherché 
\ dans PROG-MEM, si ce no de ligne
\ existe déjà src-line remplace 
\ la ligne existante sinon
\ la nouvelle ligne est insérée 
\ dans l'ordre numérique.
\ une ligne vide existante est supprimée. 
: save-line ( no-ligne -- )
	search-line
	IFF DUP del-line ENDIF
	skip-spaces ( n addr src-in src-cnt ) DUP IFF
		insert-line 
	ELSE 
		2DROP 2DROP 
	ENDIF 
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
	>R tb-src-update R>  
; 

\ restitue le dernier caractère retiré.
\ modifie tb-src
\ entrée:
\	c   dernier carctère retourné par next-char
: unget-char ( c --  )
	IFF 
		tb-src@
		1+ SWAP 1- SWAP
		tb-src-update
	ENDIF
; 

\ retourne vrai si 
\ c est une lettre
: alpha? ( c --  f )
	TOUPPER DUP
	[CHAR] A >= 
	SWAP [CHAR] Z <=
	AND 
; 

\ retourne vrai si 
\ c est un chiffre decimal
: digit? ( c -- f )
	DUP [CHAR] 0 >=
	SWAP [CHAR] 9 <= 
	AND  
;

\ autres charactères acceptés 
\ dans les noms {'.','?','_'}
: other-valid? ( c-- f )
	DUP [CHAR] . = SWAP 
	DUP [CHAR] ? = SWAP 
	[CHAR] _ = 
	OR OR
;

: name-char? ( c -- f )
	DUP alpha? SWAP DUP digit? 
	SWAP other-valid? OR OR 
;


\ convertie la chaîne en majuscules
\ in situ.
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

\ extrait un nom du texte 
\ entrées:
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
	tb-src-update 
;

\ extrait les autres types d'unité lexicales 
\ entrées:
\	c-addr u   texte source
\ sortie: 
\	IDLEX-*   identifiant type lexème 
\   c-addr2 u2  texte après le lexème 
: scan-other ( c -- c-addr u idlex )
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
	[CHAR] \ OF IDLEX-BKSLH ENDOF 
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
\ entrées:
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
: atoi16 ( c-addr u -- n )
	2>R 0. 2R> >NUMBER 
	2DROP DROP
	$FFFF AND 
	sign-extend  
; 

\ convertie la chaîne en entier 
: int-value ( c-addr u -- n 0 IDLEX-INTEGER )
	atoi16 0 IDLEX-INTEGER 
;

\ extrait le prochain lexème
\ entrée:
\  tb-src  chaîne comptée à analyser
\          mis à jour par le scanner
\ sortie:
\  lex-val IDLEX  valeur du lexème et son identifiant.
\  IDLEX-NULL     analyse tb-src complétée.
: next-lex (  --  [lex-val IDLEX | IDLEX-NULL] )   
	ungot@ ?DUP 0= IFF 
		skip-spaces ( -- src-in src-cnt )
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
\ entrée:
\    idlex   identifiant lexème attendue
\ sortie:
\   val-lo val-hi idlex  paramètres du lexème 
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
: expect-( ( -- )
	IDLEX-LPAREN expect
	DROP 2DROP
;

\ le prochain lexème
\ doit-être ')'
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
		sfind IFF
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

\ évaluation d'une comparaison  
\ relation ::= expression [ relop expression ]
\ relop ::= '>'|'<'|'='|'<>'|'>='|'<='
: rel (  -- n )
	expression 
	>R 
	next-lex 
	DUP IDLEX-EQU >= OVER IDLEX-NE <= AND IFF 
		>R 2DROP
		expression 
		2R> >R SWAP R>
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
		R> $FFFF AND sign-extend
	ENDIF
; 

' rel is relation 

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

\ affecte une valeur 
\ à une variable A..Z 
: let-var ( var-addr -- )
	IDLEX-EQU expect
	DROP 2DROP
	relation $FFFF AND SWAP I!
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
	next-lex
	DUP WHILE 
		DUP IDLEX-LABEL = IFF
			DROP sfind \ doit-être un nom de cmd 
			IFF 
				execute
			ELSE 
				ERR-UNKOWN THROW  
			ENDIF
		ELSE IDLEX-SCOL = IFF
			2DROP 
			ELSE
				ERR-SYNTAX THROW
			ENDIF  
		ENDIF
	REPEAT 
	DROP 2DROP 
;

\ for debug only
: disp-line# 
    cr ." line#" 
	line@ drop i@ . cr 
;

\ saisie d'une expression et
\ sauvegarde sa valeur dans
\ la variable
\ entrée:
\ 	var-addr   addresse de la  variable
: input-expression ( var-addr -- )
	\ sauvegarde du contexte
	line@ 2>R
	tb-src@ 2>R
	HERE CMD-BUF-SIZE ALLOT \ alloue un tampon de saisie
	CMD-BUF-SIZE read-cmd-line 
	2DUP line! 
	tb-src-update
	rel
	SWAP I! 
	\ restauration du contexte 
	2R>
	tb-src! 
	2R> 
	line!
	CMD-BUF-SIZE NEGATE ALLOT \ libère le tampon de saisie
;

\ ligne cible pour GOTO|GOSUB
: parse-target ( -- addr )
	next-lex IDLEX-INTEGER = IFF 
		DROP search-line IFF  
			SWAP DROP \ drop line#
			DUP 2 + C@
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

\ BASIC KEY var
: KEY ( -- c )
	IDLEX-VAR expect 
	2drop 
	KEY SWAP I!
;

: KEY? ( -- f )
	KEY?
;

\ BASIC: NOT relation
\ négation logique
: NOT ( -- f )
	relation 
	0= 
;

\ BASIC: RND(expr) 
\ retourne un entier aléatoire 
\ entre [1..expr]
: RND ( -- u )
	expect-(
	expression
	expect-)
	ABSS RANDOM 1+
; 


\ *********************
\ commandes tiny BASIC 
\ *********************

\ BASIC: END 
\ termine l'exécution du programme
: END 
	run-time-only
	0 flags !
	FLINE-DONE flags ! 
;

defer LET 
\ initialisation boucle FOR..NEXT 
\ syntaxe: FOR var=expr 
: FOR 
	LET \ initialise la variable de contrôle
	1 for-depth +! \ incrément la profondeur de boucle
; 

\ appel de sous-routine 
\ syntaxe: GOSUB expr
\ expr doit résulté en 
\ un numéro de ligne existant
: GOSUB 
	run-time-only
	r>
	parse-target
	line@
	2>r
	tb-src@ 
	2>R
	line!
	tb-src-init
	>r
; 

\ saut inconditionnel 
\ syntaxe: GOTO expr
\ expr doit résulté en 
\ un numéro de ligne existant
: GOTO 
	run-time-only
	parse-target
	line! 
	tb-src-init 
; 

\ information sur le programme.
: HI 
    CR
    ." -------------------------------------" CR 
	." Tiny BASIC V1.0" CR
	." Copyright Jacques Deschenes, 2022" CR
;

\ exécution conditionnel
\ des commande qui suivent 
\ l'expression si celle-ci 
\ est vrai 
\ syntaxe: IF relation cmd , cmd ...  
: IF ( -- )
	relation
	0= IFF 
	 tb-src@ + 0
	 tb-src-update
	ENDIF 
;

\ saisie d'une chaîne de caractère
\ syntaxe: INPUT var [,var]* |
\          INPUT "chaine"var [,"chaine"var]* 
: INPUT ( -- c-addr )
	TRUE
	BEGIN WHILE
		next-lex DUP
		CASE 
		IDLEX-QUOTE OF
			DROP TYPE
			IDLEX-VAR expect
			2DROP input-expression
			TRUE 
		ENDOF 
		IDLEX-VAR OF
			2DROP DUP
			var-name EMIT [CHAR] : EMIT 
			input-expression
			TRUE 
		ENDOF
		IDLEX-COMMA OF DROP 2DROP TRUE ENDOF
		ungot! FALSE 
		SWAP 
		ENDCASE
	REPEAT
; 

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

\ listing du programme 
\ syntaxe: LIST 
: LIST ( -- )
	cmd-line-only 
	next-lex DUP IDLEX-INTEGER = IFF
		2DROP ( -- start-line# ) 
	ELSE
		ungot!
		1 ( -- start-line# ) 
	ENDIF
	PROG-MEM 
	BEGIN
		DUP PROG-END @ < WHILE 
			DUP I@ DUP 3 PICK  >= IFF
				5 .R SPACE 
				DUP 2 + COUNT DUP >R 3 - TYPE CR
				R> +
			ELSE 
			   DROP DUP 2 + C@ + 
			ENDIF  
	REPEAT
	2DROP
;  
 
\ interpréteur Tiny BASIC
defer tb-eval

\ commande BASIC 
defer NEW 

\ charge fichier programme 
\ syntaxe: LOAD nom-fichier
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
		tb-src@ drop 0 tb-src-update 
	ENDIF 
; 

\ BASIC: NEW 
\ efface le programme em mémoire
: _NEW 
	cmd-line-only
	PROG-MEM PROG-END ! 
	0 flags !
;

' _NEW is NEW 

\ contrôle de boucle FOR..NEXT
\ incrémente la variable 
\ et compare sa valeur
\ à la limite
\ syntaxe: NEXT var 
: NEXT ( -- )  
	next-lex IDLEX-VAR <> IFF ERR-SYNTAX THROW ENDIF
	DROP 
	DUP I@ step@ DUP >R
	+ DUP ROT i! 
	limit@ R> 0> IFF
		> IFF  
			-1 for-depth +!
		ELSE 
			loop-back 
		ENDIF
	ELSE
		< IFF 
			-1 for-depth +!
		ELSE 
			loop-back
		ENDIF 
	ENDIF
; 

\ BASIC: PAUSE expr 
\ suspend l'exécution pour 
\ n millisecondes
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
			IDLEX-BKSLH OF  \ envoie le code ASCII 0..127
				DROP 2DROP  
				next-lex IDLEX-INTEGER = IFF 
					DROP 127 AND emit
				ELSE
					ERR-SYNTAX THROW
				ENDIF
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
	sp0 @ sp! 
	ERR-QUIT THROW 
;

\ BASIC: REM texte
\ commentaire 
: REM (  --  )
	tb-src@ 
	drop 0 tb-src-update 
; 

\ sortie de sous-routine 
\ syntaxe RETURN 
: RETURN 
	run-time-only
	r>
	2r>
	tb-src!
	2r> 
	line!
	>r
; 

\ evaluation ligne par ligne 
\ du programme en mémoire
: run-loop
	BEGIN 
		FRUN test-flag WHILE  \ disp-line# ( debug support )
		line-eval 
		FSTOP test-flag INVERT IFF next-line ENDIF 
	REPEAT 
;


\ BASIC: RUN [nom-fichier]
\ démarre l'exécution du programme 
\ syntaxe: RUN 
: RUN ( -- )
	cmd-line-only
	FSTOP test-flag IFF
		FRUN flags !
		restore-stop-context
		run-loop 
	ELSE 
		LOAD 
		PROG-MEM DUP PROG-END @ < IFF 
			PROG-MEM DUP 2 + c@
			line!
			tb-src-init
			-1 for-depth ! 
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
		w/o create-file THROW TOO fd-out cr .s 
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

\ BASIC: FOR var=expr TO expr [STEP expr] ... NEXT var 
\ incrément boucle FOR..NEXT 
\ détermine l'incrément de la
\ variable. Optionnel 
\ syntaxe: STEP expr 
: STEP ( -- )
	expression
	step!
    tb-src@ loop-back!
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

\ limite boucle FOR..NEXT 
\ syntaxe:  TO expr 
:  TO 
   expression
   limit!
   1 step!
   tb-src@ loop-back!
; 

\ évalue la ligne de commande 
\ si débute par no de ligne 
\ sauvegarde la ligne dans 
\ prog-space 
\ sinon exécute la commande.
: cmd-eval ( buf count --  )
	2DUP line! tb-src-update 
	next-lex
	DUP IDLEX-INTEGER = IFF
		2drop
		save-line
	ELSE
		ungot! 
		line-eval 
	ENDIF
;

' cmd-eval is tb-eval

\ user interface 	
: CMD-LINE 
	BEGIN 
		CMD-BUF CMD-BUF-SIZE 
		CR [CHAR] # EMIT 
		read-cmd-line ( buf size -- buf cnt )
		['] cmd-eval CATCH ?DUP IFF 
			DUP ERR-QUIT = IFF EXIT ENDIF 
			error
		ENDIF 
	AGAIN 
; 

		
\ lance tiny BASIC 			
: BASIC ( -- )
	HI 
	PROG-MEM PROG-END !
	utime drop seed !  
	CMD-LINE
	CR ." Sortie de tiny BASIC"
	CR ." pour supprimer tiny BASIC"
	CR ." faite: kill-basic"
	1000 MS  CR 
; 

\ démarre l'interpréteur
\ Utiliser la commande QUIT
\ pour revenir à la ligne de
\ commande gforth 
BASIC

\ pour supprimer le programme 
\ tin-basic de l'environnement
\ gforth faire KILL-BASIC 
\ à partir de la ligne de 
\ commande gforth   


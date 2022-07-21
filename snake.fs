\ jeu serpent en mode terminal texte.
\ 

REQUIRE random.fs 

\ constantes
128 CONSTANT max-len \ longueur maximale du serpent
\ directions deplacement
0 CONSTANT east
1 CONSTANT south
2 CONSTANT west
3 CONSTANT north
78 CONSTANT play-width \ largeur surface jeu
22 CONSTANT play-height \ hauteur surface jeu
2 CONSTANT x-offset \ pour affichage
3 CONSTANT y-offset \ pour affichage
CHAR S CONSTANT ar_left \ vire a gauche
CHAR D CONSTANT ar_right \ vire a droite

\ variables
VARIABLE score \ pointage
VARIABLE head \ direction serpent
VARIABLE snake-len \ longueur serpent
VARIABLE food \ localisation pastille nourriture
VARIABLE tail \ localisation ajout anneau serpent
VARIABLE speed \ controle vitesse serpent  
VARIABLE energy \ réserve d'énergie du serpent 

\ vector permet de creer des variables tableau 1D
: vector CREATE CELLS ALLOT DOES> SWAP CELLS + ;
\ variables tableaux
4 vector c-head \ contient les caracteres de tete serpent
max-len vector snake \ le corps du serpent

\ initialisation c-head
CHAR < east c-head ! \ tete direction est
CHAR W south c-head ! \ tete direction sud
CHAR > west c-head ! \ tete direction ouest
CHAR V north c-head ! \ tete direction nord

: 2+ 
    2 + 
; 
 

\ introducteur de séquence de 
\ controle ANSI  27[
: csi ( -- )
    27 EMIT [CHAR] [ EMIT 
;

\ inverse l'affichage video 
\ FALSE  blanc/noir
\ TRUE  noir/blanc 
: b/w  ( flag -- ) 
    csi 
    7 AND [CHAR] 0 + EMIT 
    [CHAR] m EMIT 
;

\ set cursor shape 
: cursor  ( -- )  
    csi [CHAR] 0 + EMIT SPACE [CHAR] q EMIT
; 

\ efface à partir du curseur 
\ jusqu'à la fin de la ligne.
: clreol ( -- )
    csi [CHAR] K EMIT 
; 

\ fonctions graphiques
\ conversion entier non signe vers couple {x,y}
: ucoord>xy ( u -- x y )
   256 /MOD 
;

\ conversion couple {x,y} vers ucoord
: xy>ucoord ( x y -- u )
   127 AND 256 * SWAP 127 AND + 
;

\ imprime le caractère  'c' à la position {x,y}
: xy-put ( c x y -- )
   y-offset + SWAP x-offset + SWAP AT-XY EMIT 
;

\ imprime un anneau du serpent à la position 'u'
: draw-ring ( u -- )
    TRUE b/w
    [CHAR] 0 SWAP ucoord>xy xy-put
    FALSE b/w 
;


\ whiteln ( n -- )
\ dessine une ligne en inverse vidéo 
\ n est le numéro de la ligne 
: whiteln 
    1 SWAP AT-XY
    TRUE b/w 
    play-width  2+ SPACES 
    FALSE b/w 
;

\ dessine les bandes de l'arene
: draw-walls ( -- )
    PAGE 
    y-offset 1- whiteln \ top border
    play-height y-offset + whiteln \ bottom border
    TRUE b/w 
    play-height y-offset + >R y-offset
    BEGIN DUP R@ < WHILE 
        1 OVER AT-XY SPACE 
        play-width x-offset + OVER AT-XY SPACE 
        1+ 
    REPEAT 
    R>
    2DROP  
    FALSE b/w 
;

\ dessine la tête
: draw-head ( -- )
    head @ c-head @
    0 snake @ 
    ucoord>xy 
    xy-put 
;

\ dessine le serpent
: draw-snake ( -- )
    draw-head 
    snake-len @ >R 
    1 BEGIN 
        DUP R@ < WHILE 
        DUP snake @ draw-ring 
        1+ 
        REPEAT
    R> 
    2DROP
    1 26 AT-XY  
;

\ dessine la barre d'énergie 
\ utilise la variable 'energy' 
: bar ( -- ) 
    30 1 AT-XY 
    ." ENERGY:" 
    energy @ 20 / 
    40 MIN 
    ?DUP IF  
        0 DO [CHAR] | EMIT LOOP
    ENDIF 
; 

\ affiche le status
: status ( -- )
   TRUE b/w 
   1 1 AT-XY clreol 
   ." SCORE:" score @ .
   16 1 AT-XY 
   ." LENGTH:" snake-len @ .
   bar 
   FALSE b/w 
;

\ vérifie si la coordonnée 
\ coïncide avec le segment 'c'
\ du serpent.
: snake-body? ( u c -- u c f ) 
    DUP snake @ 2 PICK =  
;

\ Lors de la creation d'une pastille il faut valider
\ qu'elle ne superpose pas au serpent.
: valid-food? ( u -- f )
    TRUE SWAP snake-len @ >R 0 
    BEGIN
        DUP R@ < WHILE  ( f u c )
        snake-body? IF 
            2DROP FALSE SWAP R@ ( F T L  )  
        ENDIF  
        1+
    REPEAT 
    2DROP 
    R>
    DROP 
;

\ creation d'une pastille de nourriture
: new-food ( -- )
    0 
    BEGIN 
        DROP play-width  RANDOM x-offset +  \ x
        play-height RANDOM y-offset +  \ y
        xy>ucoord DUP valid-food? 
    UNTIL 
    food ! 
;

\ verifie si le serpent se mord.
: snake-bite? ( -- f )
    FALSE 0 snake @  snake-len @ >R 1 
    BEGIN
        DUP R@ < WHILE 
        snake-body? IF 
            2DROP TRUE SWAP R@  ENDIF
        1+
    REPEAT
    DROP 
    R> 2DROP 
;


\ retourne un flag pour chaque coordonnee
\ vrai si le long d'un mur.
: borders? ( u1 -- fy fx )
   ucoord>xy DUP 0= SWAP play-height 1- = OR
   SWAP DUP 0= SWAP play-width 1- = OR 
;

\ ajuste SCORE
: score+ ( -- )
   1 food @ borders?
   IF SWAP 2* SWAP ENDIF
   IF 2* ENDIF
   score +! TRUE food ! 
   score @ 10 MOD 
   0= IF -5 speed +! ENDIF 
;

\ rallonge le serpent
: snake+ ( -- )
   snake-len DUP >R @ DUP 1+ R> ! tail @  SWAP snake ! ;

\ dessine pastille nourriture
: draw-food  ( -- )
    [CHAR] X 
    food @
    ucoord>xy 
    xy-put 
;

\ déplace la tête du serpent 
\ u1 coordonnées actuelle de la tête 
\ u2 nouvelle coordonnées 
: move-head ( u1 -- )
    ucoord>xy
    head @ 
    DUP east = IF DROP SWAP 1+ SWAP ELSE  
    DUP south = IF DROP 1+ ELSE  
    DUP west = IF DROP SWAP 1- 255 AND SWAP ELSE  
    DROP 1-  \ north
    ENDIF ENDIF ENDIF   
    xy>ucoord 
    0 snake ! 
;

\ deplace le serpent
: move-snake ( -- )
    0 snake @ DUP
    move-head
    draw-head
    DUP draw-ring \ dessine le premier anneau  
\ déplace anneaux du serpent     
    snake-len @  >R 1 
    BEGIN ( u i -- )
        DUP R@ < WHILE 
        DUP snake @ >R DUP >R snake ! 
        R> R> SWAP  ( u i -- )
        1+
    REPEAT
    R> 2DROP     
    BL OVER ucoord>xy xy-put \ efface le dernier anneau 
    tail ! 
    1 snake @ draw-ring
    1 26 AT-XY
;

\ verification collision avec mur
: wall-bang? ( -- f )
   0 snake @ ucoord>xy  play-height 1- SWAP U<
   SWAP  play-width 1- SWAP U< OR 
;

\ verification collision
: collision? ( -- f )
   snake-bite?  
   wall-bang?  
   OR 
;

\ vérification mort de faim
: starvation? ( -- f ) 
    energy @ 0=
;

 \ initialisation du serpent
: snake-init ( -- )
   east head !
   play-width 2/ play-height 2/ xy>ucoord snake-len @  >R 0
   BEGIN 
        DUP R@ < WHILE 
        2DUP snake ! 
        SWAP 1- SWAP
        1+
    REPEAT
    2DROP 
    R> DROP  
;

\ le serpent tourne 
\ -1 à gauche 
\  1 à droite 
: turn ( -1 | 1 -- ) 
  head @ + 3 AND head !
; 


\ lecture clavier touche 'q' quitte le jeu.
: user-key? ( -- f )
    KEY? IF 
        KEY TOUPPER 
        CASE 
        [CHAR] Q OF  TRUE  ENDOF
        [CHAR] S OF  -1 turn FALSE ENDOF
        [CHAR] D OF  1 turn FALSE  ENDOF  
        FALSE SWAP  
        ENDCASE
    ELSE 
        FALSE    
    ENDIF
;  

\ pastille mangee?
: eat-food ( --  )
   0 snake @ food @ = 
   if 
        score+
        snake+
        100 energy +!
   ENDIF 
;

\ dépense d'énergie 
\ lorsque la variable 'energy' 
\ tombe à zéro la partie se termine.
: alive? ( -- f )
    -1 energy +!
    collision? starvation? or 
    INVERT 
; 


\ boucle du jeu
: game-loop ( -- )
  BEGIN 
    speed @ ms \ délais contrôle de vitesse 
    alive? IF \ serpent encore vivant  
        food @ -1 = IF new-food draw-food ENDIF 
        user-key? 
        move-snake 
        eat-food 
    ELSE 
        TRUE
    ENDIF
    status  
  UNTIL
;

\ initialisation du jeu
: game-init ( -- )
    UTIME DROP SEED ! \ initialize PRNG generator 
    70 speed ! 
    200 energy ! 
    4 snake-len ! 
    0 score ! 
    draw-walls
    status
    snake-init 
    draw-snake
    new-food 
    draw-food 
;

\ attend une touche 
\ retourne vrai si touche 'Q' 
: game-exit? (  -- f )
    1 26 AT-XY
    starvation? IF 
        ." died of starvation" CR 
    ENDIF
   ." game over <q> to leave, other to continue."
    KEY TOUPPER 
    [CHAR] Q = 
;

\ partie terminee
\ que veut faire l'utilisateur? 
: what's-next? ( -- f )
    \ vide la file du clavier
    BEGIN KEY? WHILE KEY DROP REPEAT   
    game-exit?
;

\ lance le jeux.
: snake-run ( -- )
    6 CURSOR \ change la forme du curseur pour une barre verticale
    BEGIN 
        game-init 
        game-loop 
        what's-next?
    UNTIL 
    1 CURSOR \ rétabli le curseur bloc. 
    PAGE \ efface l'écran
;

snake-run \ démarre le jeu 

bye \ quitte gforth



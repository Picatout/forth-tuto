   10 ? "ABS(-234)=",abs(-234)
   20 ? " 100 nombres aléatoires"; 
   24 for i=1 to 100 ; ? rnd(1000), ; next i ; ? 
   30 ? "Enfoncez une touche ",; KEY K ; ? \K  
   40 ? "Efoncez une touche pour continer"
   42 IF NOT KEY? THEN GOTO 42 
   43 KEY K ; REM rejette la lettre en attente. 
   44 ? 'ABS(msec)=',ABS(msec)
   50 ? 'Je me repose 1 seconde'; pause 1000
   60 ? 'Pause terminée ABS(msec)=',ABS(msec)
   70 ? #1,'Toute valeur non nulle est vrai donc: NOT -3 = ',NOT -3,
   72 ? ' et NOT 0 = ',NOT 0,#6
   80 ? 'Entendez-vous ce BEEP?' ; beep
   90 ? \13\10,"Je m'efface et je reviens"; pause 1000 ; CLS 
   92 ? 'Me revoila!'
   100 ? "LET A=31415*2%7, A = ", ; LET A=31415*2%7; ? #2,A,#6 
   110 ? "J'appelle une sous-routine"; GOSUB 1000
   120 ? "Je suis de retour de Fibo, donc GOSUB et GOTO fonctionnent."
   130 ? "La commande IF a aussi étée testée dans Fibo"
   140 PRINT "Saissisez, une expression ou une lettre et ",
   144 INPUT "je vous retourne le résultat "N 
   150 ? "La variable contient:",N 
   160 INPUT "Voulez faire une autre saisie (O|N) ? "N 
   170 IF N=ASC(O) THEN GOTO 140
   180 ? "Ceci est le dernier test ",
   190 ? "Je fais un STOP. Tapez RUN pour rapartir le programme." 
   200 STOP 
   210  ? "Je recommence après la commande STOP"
   220  ? ; ? "C'est tout! Ça semble fonctionner."
   999  END 
   1000 ? "Suite de Fibonacci"
   1010 LET A=1,B=1
   1020 ? A,
   1030 LET C=A+B,A=B,B=C 
   1040 IF A>0 THEN GOTO 1020
   1050 ? ; RETURN 

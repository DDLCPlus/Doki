label startmod:
    invoke do_custom_dialogue_box
    $ n_name = "Natsuki the Natsuki"
    define nc = DynamicCharacter('Natsuki Clone', image='clonesuki', what_prefix='"', what_suffix='"', ctc="ctc", ctc_position="fixed")
    # define nc = Character('Natsuki Clone', image='clonesuki', what_prefix='"', what_suffix='"')
    # image clonesuki 1a = "im.Composite((960, 960), (0, 0), "clonesuki/1l.png", (0, 0), "clonesuki/1r.png", (0, 0), "clonesuki/a.png")"
    # define custom_music = "custom_music.ogg"
    # image bg bg_test2 = "bg_test_original/bg_test2.png"
    # image bg normal_fucking_background = "normal_fucking_background/normal_fucking_background.jpg"
    stop music fadeout 2.0
    play music p2
    # play music custom_music

    scene bg corridor
    # scene bg bg_test2
    with dissolve_scene_full
    "As I make my way through the hall, I can't help but notice some frustrated sighs coming from the clubroom."
    scene bg club_day
    # scene bg normal_fucking_background
    #show natsuki 3o zorder 1 at t11
    #show natsuki2 3o zorder 2 at h34
    show natsuki 3o zorder 2 at t22
    nc "This is a test I'm a clone!"
    # show clonesuki 1a zorder 3 at f21
    n "Ugh why won't it work?"
    hide natsuki
    scene bg corridor
    "Through the window of the door I notice natsuki standing next to what looks like some big contraption."
    "What the hell is that girl up to? First cupcakes and now engineering? Has she lost her mind?"
    scene bg club_day
    with dissolve_scene_full
    
    mc "Hey Natsuki, what are you doing?"
    show natsuki 1p zorder 1 at h43
    n "OH SHIT-"
    hide natsuki
    show natsuki 3i zorder 1 at h43
    n "God.. don't do that dummy, you scared me."
    n "I'm currently trying to get the cloninator 9000 to work.."
    mc "Cloninator?"
    hide natsuki
    show natsuki 3j zorder 1 at t11
    n "Yep! Made it myself. Basically, this bad boy takes me and makes another me - I did it for help with like baking and stuff"
    hide natsuki
    show natsuki 2q zorder 1 at t11
    n "Although.. It doesn't seem to be working right now.."
    mc "I see. Have you tried pushing the on button?"

    stop music
    invoke fix_menu_stuff
    jump end_game
    return